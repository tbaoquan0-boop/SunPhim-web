/**
 * SunPhim - API & Storage Management Layer
 * High performance, zero latency client state & image resolver
 */

'use strict';

const API = {
    baseUrl: '/api',

    async request(url, options = {}) {
        try {
            const response = await fetch(url, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });

            if (!response.ok) {
                const text = await response.text().catch(() => '');
                throw new Error(text || `HTTP ${response.status}: ${response.statusText}`);
            }

            return await response.json();
        } catch (error) {
            console.warn('[SunPhim API Warning]', error);
            const msg = error instanceof Error ? error.message : String(error ?? 'Lỗi kết nối máy chủ');
            throw new Error(msg);
        }
    },

    home: {
        async getHomePage() {
            return API.request(`${API.baseUrl}/home`);
        },
        async getFeatured() {
            return API.request(`${API.baseUrl}/home/featured`);
        },
        async getTrending(limit = 24) {
            return API.request(`${API.baseUrl}/home/trending?limit=${limit}`);
        },
        async getNewReleases(limit = 24) {
            return API.request(`${API.baseUrl}/home/new-releases?limit=${limit}`);
        },
        async getTopRated(limit = 24) {
            return API.request(`${API.baseUrl}/home/top-rated?limit=${limit}`);
        },
        async getHeroBanner() {
            return API.request(`${API.baseUrl}/home/hero-banner`);
        }
    },

    movies: {
        async getAll(type = null) {
            const url = type ? `${API.baseUrl}/movies?type=${type}` : `${API.baseUrl}/movies`;
            return API.request(url);
        },
        async getBySlug(slug) {
            return API.request(`${API.baseUrl}/movies/slug/${slug}`);
        },
        async getByCategory(categorySlug) {
            return API.request(`${API.baseUrl}/movies/category/${categorySlug}`);
        },
        async search(keyword) {
            return API.request(`${API.baseUrl}/movies/search?keyword=${encodeURIComponent(keyword)}`);
        }
    },

    categories: {
        async getAll() {
            return API.request(`${API.baseUrl}/categories`);
        }
    }
};

const UTILS = {
    formatNumber(num) {
        if (!num) return '0';
        if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
        if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
        return num.toString();
    },

    formatDate(dateStr) {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        return date.toLocaleDateString('vi-VN', { day: '2-digit', month: 'short', year: 'numeric' });
    },

    truncate(str, maxLen = 150) {
        if (!str) return '';
        return str.length > maxLen ? str.slice(0, maxLen) + '...' : str;
    },

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func(...args), wait);
        };
    },

    // Fast image resolvers (Direct CDN with no-referrer)
    movieThumbById(movie) {
        return movie?.thumb || movie?.poster || '/img/placeholder.svg';
    },

    moviePosterById(movie) {
        return movie?.poster || movie?.thumb || '/img/placeholder.svg';
    },

    buildImageUrl(url) {
        return url || '/img/placeholder.svg';
    },

    // =========================================================================
    // WATCHLIST MANAGEMENT (DANH SÁCH CỦA TÔI)
    // =========================================================================
    getWatchlist() {
        try {
            const data = localStorage.getItem('sunphim_watchlist');
            return data ? JSON.parse(data) : [];
        } catch {
            return [];
        }
    },

    isInWatchlist(slug) {
        if (!slug) return false;
        const list = UTILS.getWatchlist();
        return list.some(item => item.slug === slug);
    },

    addToWatchlist(movie) {
        if (!movie || !movie.slug) return false;
        const list = UTILS.getWatchlist();
        if (!list.some(item => item.slug === movie.slug)) {
            list.unshift({
                slug: movie.slug,
                name: movie.name,
                originName: movie.originName || '',
                thumb: movie.thumb || movie.poster || '',
                poster: movie.poster || movie.thumb || '',
                quality: movie.quality || '4K Ultra HD',
                rating: movie.rating || movie.imdbScore || 8.5,
                year: movie.year || 2024,
                type: movie.type || 'single',
                savedAt: Date.now()
            });
            localStorage.setItem('sunphim_watchlist', JSON.stringify(list));
            UTILS.showToast(`Đã thêm "${movie.name}" vào danh sách yêu thích`, 'success');
            return true;
        }
        return false;
    },

    removeFromWatchlist(slug) {
        if (!slug) return false;
        let list = UTILS.getWatchlist();
        const initialLength = list.length;
        list = list.filter(item => item.slug !== slug);
        if (list.length !== initialLength) {
            localStorage.setItem('sunphim_watchlist', JSON.stringify(list));
            UTILS.showToast('Đã xóa khỏi danh sách yêu thích', 'info');
            return true;
        }
        return false;
    },

    toggleWatchlist(movie) {
        if (UTILS.isInWatchlist(movie.slug)) {
            UTILS.removeFromWatchlist(movie.slug);
            return false;
        } else {
            UTILS.addToWatchlist(movie);
            return true;
        }
    },

    // =========================================================================
    // CONTINUE WATCHING MANAGEMENT (TIẾP TỤC XEM)
    // =========================================================================
    getContinueWatchingList() {
        try {
            const data = localStorage.getItem('sunphim_continue_watching');
            return data ? JSON.parse(data) : [];
        } catch {
            return [];
        }
    },

    saveContinueWatching(movie, episode, progress = 50, durationText = '') {
        if (!movie || !movie.slug) return;
        try {
            let list = UTILS.getContinueWatchingList();
            list = list.filter(item => item.slug !== movie.slug);

            list.unshift({
                slug: movie.slug,
                name: movie.name,
                thumb: movie.thumb || movie.poster || '',
                poster: movie.poster || movie.thumb || '',
                episodeId: episode?.id || 1,
                episodeName: episode?.name || `Tập ${episode?.episodeNumber || 1}`,
                progress: Math.min(Math.max(progress, 5), 95),
                durationText: durationText || 'Còn 35 phút',
                updatedAt: Date.now()
            });

            // Keep max 20 recent items
            localStorage.setItem('sunphim_continue_watching', JSON.stringify(list.slice(0, 20)));
        } catch (e) {
            console.error('Error saving continue watching:', e);
        }
    },

    removeContinueWatching(slug) {
        try {
            let list = UTILS.getContinueWatchingList();
            list = list.filter(item => item.slug !== slug);
            localStorage.setItem('sunphim_continue_watching', JSON.stringify(list));
            UTILS.showToast('Đã xóa khỏi lịch sử tiếp tục xem', 'info');
        } catch (e) {
            console.error('Error removing continue watching:', e);
        }
    },

    // =========================================================================
    // TOAST SYSTEM
    // =========================================================================
    showToast(message, type = 'info') {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;

        const iconSvg = type === 'success'
            ? `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="16" height="16"><polyline points="20 6 9 17 4 12"/></svg>`
            : type === 'error'
            ? `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="16" height="16"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>`
            : `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="16" height="16"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>`;

        toast.innerHTML = `${iconSvg} <span>${message}</span>`;
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.animation = 'toastIn 0.3s ease reverse';
            setTimeout(() => toast.remove(), 280);
        }, 3200);
    }
};

if (typeof window !== 'undefined') {
    window.API = API;
    window.UTILS = UTILS;
}
