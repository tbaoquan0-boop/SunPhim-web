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
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    },

    home: {
        async getHomePage() {
            return API.request(`${API.baseUrl}/home`);
        },

        async getFeatured() {
            return API.request(`${API.baseUrl}/home/featured`);
        },

        async getTrending(limit = 20) {
            return API.request(`${API.baseUrl}/home/trending?limit=${limit}`);
        },

        async getNewReleases(limit = 20) {
            return API.request(`${API.baseUrl}/home/new-releases?limit=${limit}`);
        },

        async getTopRated(limit = 20) {
            return API.request(`${API.baseUrl}/home/top-rated?limit=${limit}`);
        },

        async getRandomFeatured(count = 5) {
            return API.request(`${API.baseUrl}/home/random-featured?count=${count}`);
        },

        async getByCategory(limit = 12) {
            return API.request(`${API.baseUrl}/home/by-category?limit=${limit}`);
        },

        async getHeroBanner() {
            return API.request(`${API.baseUrl}/home/hero-banner`);
        },

        async getContinueWatching(userId, limit = 10) {
            return API.request(`${API.baseUrl}/home/continue-watching?userId=${userId}&limit=${limit}`);
        }
    },

    movies: {
        async getAll(type = null) {
            const url = type ? `${API.baseUrl}/movies?type=${type}` : `${API.baseUrl}/movies`;
            return API.request(url);
        },

        async getById(id) {
            return API.request(`${API.baseUrl}/movies/${id}`);
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
        },

        async getFeatured() {
            return API.request(`${API.baseUrl}/categories/featured`);
        },

        async getBySlug(slug) {
            return API.request(`${API.baseUrl}/categories/slug/${slug}`);
        }
    },

    streaming: {
        async getPlayerUrl(episodeId) {
            return API.request(`${API.baseUrl}/stream/player/${episodeId}?proxy=true`);
        },

        async reportBroken(url) {
            return API.request(`${API.baseUrl}/stream/report`, {
                method: 'POST',
                body: JSON.stringify({ url })
            });
        }
    },

    image: {
        proxyUrl(url, width = 500) {
            return `${API.baseUrl}/image/proxy?url=${encodeURIComponent(url)}&width=${width}`;
        }
    }
};

const UTILS = {
    formatNumber(num) {
        if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
        if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
        return num?.toString() || '0';
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
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    getUserId() {
        let userId = localStorage.getItem('sunphim_user_id');
        if (!userId) {
            userId = 'user_' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('sunphim_user_id', userId);
        }
        return userId;
    },

    setEpisodeProgress(movieId, episodeId, progress) {
        const key = `progress_${movieId}`;
        localStorage.setItem(key, JSON.stringify({
            episodeId,
            progress,
            timestamp: Date.now()
        }));
    },

    getEpisodeProgress(movieId) {
        const key = `progress_${movieId}`;
        const data = localStorage.getItem(key);
        return data ? JSON.parse(data) : null;
    },

    showToast(message, type = 'info') {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.animation = 'toast-in 0.3s ease reverse';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    },

    buildImageUrl(url, width = 300) {
        if (!url) return '/img/placeholder.svg';

        if (url.startsWith('http')) {
            // Cho phep load truc tiep tu cac host duoc phep
            const allowedHosts = [
                'img.ophim.live',
                'image.tmdb.org',
                'm.media-imdb.com',
                'upload.wikimedia.org',
                'vignette.wikia.nocookie.net'
            ];
            const hostname = new URL(url).hostname;
            if (allowedHosts.some(h => hostname === h || hostname.endsWith('.' + h))) {
                return url;
            }
            // Nhung host khac thi qua proxy
            return API.image.proxyUrl(url, width);
        }
        return url;
    },

    getRatingClass(score) {
        if (!score) return '';
        if (score >= 8) return 'high';
        if (score >= 6) return 'medium';
        return 'low';
    }
};
