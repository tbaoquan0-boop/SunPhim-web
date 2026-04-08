// ==============================
// SunPhim Netflix-Style UI
// ==============================

// Global state
const STATE = {
    homeData: null,
    currentMovie: null,
    currentEpisodes: [],
    currentEpisodeIndex: 0
};

// ==============================
// MOVIE CARD BUILDER
// ==============================
function buildMovieCard(movie) {
    const isNew = movie.year && new Date(movie.year, 0, 1) > new Date(Date.now() - 365 * 24 * 60 * 60 * 1000);
    const episodeText = movie.type === 'series' && movie.episodeCount > 0
        ? `${movie.episodeCount} tập`
        : null;

    return `
        <div class="movie-card" data-slug="${movie.slug}" onclick="navigateToMovie('${movie.slug}')">
            <div class="movie-card-poster">
                <img src="${UTILS.buildImageUrl(movie.thumb || movie.poster, 300)}"
                     alt="${escapeHtml(movie.name)}"
                     loading="lazy"
                     onerror="this.src='/img/placeholder.svg'">
                <div class="movie-card-overlay">
                    <p class="movie-card-title">${escapeHtml(movie.name)}</p>
                    <div class="movie-card-meta">
                        ${movie.quality ? `<span class="tag">${movie.quality}</span>` : ''}
                        ${movie.lang ? `<span class="tag">${movie.lang}</span>` : ''}
                        ${episodeText ? `<span class="tag">${episodeText}</span>` : ''}
                    </div>
                </div>
                <div class="movie-card-badges">
                    ${movie.quality ? `<span class="badge badge-quality">${movie.quality}</span>` : ''}
                    ${isNew ? `<span class="badge badge-new">Mới</span>` : ''}
                </div>
                ${movie.imdbScore ? `
                <div class="badge-rating">
                    <svg viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
                    ${movie.imdbScore.toFixed(1)}
                </div>` : ''}
            </div>
        </div>
    `;
}

// ==============================
// MOVIE ROW BUILDER
// ==============================
function buildMovieRowContent(movies) {
    if (!movies || movies.length === 0) {
        return '<div class="search-no-results" style="padding: 1rem 0;">Chưa có phim nào</div>';
    }
    return movies.map(buildMovieCard).join('');
}

// ==============================
// HERO SECTION
// ==============================
function renderHero(movie) {
    if (!movie) {
        document.getElementById('heroSection').style.display = 'none';
        return;
    }

    const backdropUrl = UTILS.buildImageUrl(movie.poster || movie.thumb, 1920);
    document.getElementById('heroBackdrop').style.backgroundImage = `url('${backdropUrl}')`;

    document.getElementById('heroTitle').textContent = movie.name;

    const metaHtml = [
        movie.imdbScore ? `<span class="hero-rating">
            <svg viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
            ${movie.imdbScore.toFixed(1)}
        </span>` : '',
        movie.year ? `<span>${movie.year}</span>` : '',
        movie.quality ? `<span>${movie.quality}</span>` : '',
        movie.lang ? `<span>${movie.lang}</span>` : '',
        movie.episodeCount > 0 ? `<span>${movie.episodeCount} tập</span>` : ''
    ].filter(Boolean).join('<span style="opacity:0.5">|</span>');

    document.getElementById('heroMeta').innerHTML = metaHtml;

    document.getElementById('heroDescription').textContent = movie.description
        ? UTILS.truncate(movie.description, 200)
        : `Xem phim ${movie.name} online chất lượng cao, vietsub, thuyết minh.`;

    const tagsHtml = movie.categories?.slice(0, 4).map(c =>
        `<span class="tag">${escapeHtml(c)}</span>`
    ).join('') || '';

    document.getElementById('heroTags').innerHTML = tagsHtml;

    document.getElementById('heroPlayBtn').onclick = () => {
        // Luon mo trang chi tiet: phim le can episode id thuc (khong dung episode=0)
        window.location.href = `/movie.html?slug=${movie.slug}`;
    };

    document.getElementById('heroInfoBtn').onclick = () => {
        window.location.href = `/movie.html?slug=${movie.slug}`;
    };

    // Update page title
    document.title = `${movie.name} - SunPhim`;
}

// ==============================
// HOMEPAGE LOADER
// ==============================
async function loadHomePage() {
    try {
        showLoadingState();

        const data = await API.home.getHomePage();
        STATE.homeData = data;

        const heroMovie = data.featured?.[0] || data.trending?.[0] || data.newReleases?.[0] || null;
        renderHero(heroMovie);

        document.getElementById('trending').innerHTML = buildMovieRowContent(data.trending);
        document.getElementById('newReleases').innerHTML = buildMovieRowContent(data.newReleases);
        document.getElementById('topRated').innerHTML = buildMovieRowContent(data.topRated);
        document.getElementById('series').innerHTML = buildMovieRowContent(data.seriesList);
        document.getElementById('singles').innerHTML = buildMovieRowContent(data.singleMovies);

        // Category sections
        if (data.categories?.length > 0) {
            const catHtml = data.categories.map(cat => `
                <div class="category-section">
                    <h3 class="category-title">${escapeHtml(cat.name)}</h3>
                    <div class="movie-row">
                        <button class="movie-row-nav nav-prev" data-target="cat-${cat.id}" aria-label="Lùi">‹</button>
                        <div class="movie-row-inner" id="cat-${cat.id}">${cat.movies?.map(buildMovieCard).join('') || ''}</div>
                        <button class="movie-row-nav nav-next" data-target="cat-${cat.id}" aria-label="Tiến">›</button>
                    </div>
                </div>
            `).join('');

            document.getElementById('categorySections').innerHTML = catHtml;
        }

        initNavButtons();
        initSearchHandlers();
    } catch (error) {
        console.error('[SunPhim] Load home page failed:', error);
        UTILS.showToast('Không thể tải dữ liệu. Kiểm tra API /api/home và console.', 'error');
        const hero = document.getElementById('heroSection');
        if (hero) hero.style.display = 'block';
        const titleEl = document.getElementById('heroTitle');
        if (titleEl) titleEl.textContent = 'Không tải được dữ liệu';
        const descEl = document.getElementById('heroDescription');
        if (descEl) {
            descEl.textContent = error?.message
                ? String(error.message)
                : 'Gọi GET /api/home thất bại. Mở tab Network xem request home (JSON).';
        }
    }
}

function showLoadingState() {
    const placeholder = (count = 6) => Array(count).fill('').map(() =>
        `<div class="skeleton skeleton-card"></div>`
    ).join('');

    const sections = ['trending', 'newReleases', 'topRated', 'series', 'singles'];
    sections.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerHTML = placeholder();
    });
}

// ==============================
// MOVIE DETAIL LOADER
// ==============================
async function loadMovieDetail(slug) {
    try {
        const movie = await API.movies.getBySlug(slug);

        if (!movie) {
            document.getElementById('detailTitle').textContent = 'Không tìm thấy phim';
            return;
        }

        STATE.currentMovie = movie;
        STATE.currentEpisodes = movie.episodes || [];
        STATE.currentEpisodeIndex = 0;

        // Hero background
        const backdropUrl = UTILS.buildImageUrl(movie.poster || movie.thumb, 1920);
        document.getElementById('detailHero').style.backgroundImage = `url('${backdropUrl}')`;

        // SEO
        document.getElementById('pageTitle').textContent = `${movie.name} - SunPhim`;
        document.getElementById('metaDescription').setAttribute('content', movie.metaDescription || movie.description || '');
        document.getElementById('ogTitle').setAttribute('content', movie.ogTitle || movie.name);
        document.getElementById('ogDescription').setAttribute('content', movie.ogDescription || movie.description || '');
        document.getElementById('ogImage').setAttribute('content', UTILS.buildImageUrl(movie.ogImage || movie.poster, 1200));

        if (movie.schemaMarkup) {
            document.getElementById('schemaMarkup').textContent = movie.schemaMarkup;
        }

        // Poster
        document.getElementById('detailPoster').src = UTILS.buildImageUrl(movie.poster || movie.thumb, 600);
        document.getElementById('detailPoster').alt = movie.name;

        // Title
        document.getElementById('detailTitle').textContent = movie.name;
        if (movie.originName) {
            document.getElementById('detailOriginName').textContent = movie.originName;
        }

        // Meta
        const metaHtml = [
            movie.imdbScore ? `<span class="detail-rating">
                <svg viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
                ${movie.imdbScore.toFixed(1)} / 10
            </span>` : '',
            movie.year ? `<span class="detail-meta-item"><span>📅</span> ${movie.year}</span>` : '',
            movie.duration ? `<span class="detail-meta-item"><span>⏱️</span> ${movie.duration} phút</span>` : '',
            movie.quality ? `<span class="detail-meta-item"><span>📺</span> ${movie.quality}</span>` : '',
            movie.lang ? `<span class="detail-meta-item"><span>🔊</span> ${movie.lang}</span>` : '',
            movie.viewCount ? `<span class="detail-meta-item"><span>👁️</span> ${UTILS.formatNumber(movie.viewCount)} lượt xem</span>` : ''
        ].filter(Boolean).join('');

        document.getElementById('detailMeta').innerHTML = metaHtml;

        // Tags
        const tagsHtml = movie.categories?.map(c =>
            `<a href="/the-loai/${c.toLowerCase().replace(/\s+/g, '-')}" class="tag">${escapeHtml(c)}</a>`
        ).join('') || '';

        document.getElementById('detailTags').innerHTML = tagsHtml;

        // Actions
        const firstEpisode = STATE.currentEpisodes[0];
        const playUrl = firstEpisode
            ? `/watch.html?movie=${slug}&episode=${firstEpisode.id}`
            : '#';

        const actionsHtml = `
            <a href="${playUrl}" class="btn btn-primary" id="playBtn">
                <svg viewBox="0 0 24 24" fill="currentColor"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                Xem phim
            </a>
            <button class="btn btn-secondary" onclick="window.history.back()">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M12 5l-7 7 7 7"/></svg>
                Quay lại
            </button>
        `;

        document.getElementById('detailActions').innerHTML = actionsHtml;

        // Description
        document.getElementById('detailDescription').textContent = movie.description || 'Chưa có mô tả cho phim này.';

        // Episodes
        renderEpisodes(movie, slug);
        initSearchHandlers();
    } catch (error) {
        console.error('Load movie detail failed:', error);
        UTILS.showToast('Không thể tải thông tin phim. Vui lòng thử lại.', 'error');
    }
}

function renderEpisodes(movie, slug) {
    const grid = document.getElementById('episodesGrid');

    if (!movie.episodes || movie.episodes.length === 0) {
        grid.innerHTML = '<div class="search-no-results">Chưa có tập phim nào được cập nhật</div>';
        document.getElementById('episodesSection').style.display = 'none';
        return;
    }

    const title = movie.type === 'series'
        ? `Danh sách tập (${movie.episodes.length} tập)`
        : 'Thông tin phim';

    document.getElementById('episodesTitle').textContent = title;

    grid.innerHTML = movie.episodes.map((ep, idx) => `
        <div class="episode-card" data-episode-id="${ep.id}" data-index="${idx}" onclick="playEpisode('${slug}', ${ep.id}, ${idx})">
            <div class="episode-thumb">
                <img src="${UTILS.buildImageUrl(movie.thumb, 200)}"
                     alt="${escapeHtml(ep.name)}"
                     loading="lazy"
                     onerror="this.style.background='var(--bg-tertiary)'">
            </div>
            <div class="episode-info">
                <p class="episode-name">${escapeHtml(ep.name)}</p>
                <p class="episode-number">Tập ${ep.episodeNumber}</p>
            </div>
        </div>
    `).join('');
}

function playEpisode(movieSlug, episodeId, index) {
    STATE.currentEpisodeIndex = index;
    window.location.href = `/watch.html?movie=${movieSlug}&episode=${episodeId}`;
}

// ==============================
// EPISODE PLAYER LOADER
// ==============================
async function loadEpisodePlayer(movieSlug, episodeId) {
    try {
        document.getElementById('playerTitle').textContent = 'Đang tải player...';

        const movie = await API.movies.getBySlug(movieSlug);

        if (!movie) {
            showPlayerError('Không tìm thấy phim');
            return;
        }

        STATE.currentMovie = movie;
        STATE.currentEpisodes = movie.episodes || [];

        const episode = STATE.currentEpisodes.find(e => e.id === episodeId);
        if (!episode) {
            showPlayerError('Không tìm thấy tập phim');
            return;
        }

        const epIndex = STATE.currentEpisodes.indexOf(episode);
        STATE.currentEpisodeIndex = epIndex;

        document.getElementById('playerTitle').textContent = `${movie.name} - ${episode.name}`;

        // Build episode mini list
        const miniHtml = STATE.currentEpisodes.map((ep, idx) => `
            <div class="episode-card ${idx === epIndex ? 'active' : ''}"
                 data-episode-id="${ep.id}"
                 onclick="playEpisodeFromPlayer('${movieSlug}', ${ep.id}, ${idx})">
                <div class="episode-thumb">
                    <img src="${UTILS.buildImageUrl(movie.thumb, 200)}"
                         alt="${escapeHtml(ep.name)}"
                         loading="lazy">
                </div>
                <div class="episode-info">
                    <p class="episode-name">${escapeHtml(ep.name)}</p>
                    <p class="episode-number">Tập ${ep.episodeNumber}</p>
                </div>
            </div>
        `).join('');

        document.getElementById('episodesMiniList').innerHTML = `
            <h3 style="margin: 1.5rem 0 1rem; font-size: 1.2rem;">Danh sách tập</h3>
            <div class="episodes-grid">${miniHtml}</div>
        `;

        // Update nav buttons
        document.getElementById('prevEpisode').disabled = epIndex <= 0;
        document.getElementById('nextEpisode').disabled = epIndex >= STATE.currentEpisodes.length - 1;

        document.getElementById('prevEpisode').onclick = () => {
            if (epIndex > 0) {
                const prevEp = STATE.currentEpisodes[epIndex - 1];
                window.location.href = `/watch.html?movie=${movieSlug}&episode=${prevEp.id}`;
            }
        };

        document.getElementById('nextEpisode').onclick = () => {
            if (epIndex < STATE.currentEpisodes.length - 1) {
                const nextEp = STATE.currentEpisodes[epIndex + 1];
                window.location.href = `/watch.html?movie=${movieSlug}&episode=${nextEp.id}`;
            }
        };

        // Load player
        await loadPlayer(episode);

        // Update page title
        document.title = `${movie.name} - ${episode.name} - SunPhim`;
    } catch (error) {
        console.error('Load player failed:', error);
        showPlayerError('Không thể tải video. Vui lòng thử lại.');
    }
}

async function loadPlayer(episode) {
    const wrapper = document.getElementById('playerWrapper');

    if (episode.embedLink) {
        // Embed (YouTube / host khac) — can mo rong allow cho trinh duyet / Cốc Cốc
        wrapper.innerHTML = `
            <iframe
                src="${episode.embedLink}"
                title="Player"
                allowfullscreen
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share; fullscreen"
                referrerpolicy="strict-origin-when-cross-origin"
                loading="lazy">
            </iframe>
        `;
    } else if (episode.fileUrl) {
        // Try to get protected stream URL
        try {
            const playerData = await API.streaming.getPlayerUrl(episode.id);

            if (playerData.playerUrl) {
                wrapper.innerHTML = `
                    <iframe
                        src="${playerData.playerUrl}"
                        allowfullscreen
                        allow="autoplay; fullscreen"
                        referrerpolicy="strict-origin-when-cross-origin">
                    </iframe>
                `;
            } else if (episode.fileUrl.includes('.m3u8')) {
                // Use HLS.js for m3u8 streams
                wrapper.innerHTML = `
                    <video id="hlsVideo" controls playsinline>
                        <source src="${episode.fileUrl}" type="application/x-mpegURL">
                    </video>
                `;
                initHlsPlayer(episode.fileUrl);
            } else {
                // Direct video
                wrapper.innerHTML = `
                    <video id="directVideo" controls playsinline autoplay>
                        <source src="${episode.fileUrl}" type="video/mp4">
                    </video>
                `;
            }
        } catch {
            // Fallback: show embed or error
            if (episode.fileUrl.includes('m3u8')) {
                wrapper.innerHTML = `
                    <video id="hlsVideo" controls playsinline>
                        <source src="${episode.fileUrl}" type="application/x-mpegURL">
                    </video>
                `;
                initHlsPlayer(episode.fileUrl);
            } else {
                wrapper.innerHTML = `
                    <video id="directVideo" controls playsinline autoplay>
                        <source src="${episode.fileUrl}" type="video/mp4">
                    </video>
                `;
            }
        }
    } else {
        showPlayerError('Chưa có nguồn phát cho tập này');
    }
}

function initHlsPlayer(url) {
    const videoEl = document.getElementById('hlsVideo');
    if (!videoEl || typeof Hls === 'undefined') {
        // Try native HLS on Safari
        return;
    }

    if (Hls.isSupported()) {
        const hls = new Hls();
        hls.loadSource(url);
        hls.attachMedia(videoEl);
    } else if (videoEl.canPlayType('application/vnd.apple.mpegurl')) {
        videoEl.src = url;
    }
}

function showPlayerError(message) {
    document.getElementById('playerWrapper').innerHTML = `
        <div class="player-error">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/>
                <line x1="15" y1="9" x2="9" y2="15"/>
                <line x1="9" y1="9" x2="15" y2="15"/>
            </svg>
            <p>${escapeHtml(message)}</p>
        </div>
    `;
}

function playEpisodeFromPlayer(movieSlug, episodeId, index) {
    STATE.currentEpisodeIndex = index;
    window.location.href = `/watch.html?movie=${movieSlug}&episode=${episodeId}`;
}

// ==============================
// NAVIGATION
// ==============================
function navigateToMovie(slug) {
    window.location.href = `/movie.html?slug=${slug}`;
}

// ==============================
// SEARCH
// ==============================
function initSearchHandlers() {
    const searchInput = document.getElementById('searchInput');
    const searchBtn = document.getElementById('searchBtn');
    const searchOverlay = document.getElementById('searchOverlay');
    const searchOverlayInput = document.getElementById('searchOverlayInput');
    const searchResults = document.getElementById('searchResults');
    const searchClose = document.querySelector('.search-close');

    if (searchBtn) {
        searchBtn.onclick = () => {
            if (searchOverlay) {
                searchOverlay.classList.add('active');
                if (searchOverlayInput) searchOverlayInput.focus();
            }
        };
    }

    if (searchOverlayInput) {
        searchOverlayInput.addEventListener('input', UTILS.debounce(async (e) => {
            const keyword = e.target.value.trim();
            if (keyword.length < 2) {
                searchResults.innerHTML = '<div class="search-no-results">Nhập ít nhất 2 ký tự để tìm kiếm</div>';
                return;
            }

            try {
                const results = await API.movies.search(keyword);
                if (results.length === 0) {
                    searchResults.innerHTML = '<div class="search-no-results">Không tìm thấy kết quả nào</div>';
                } else {
                    searchResults.innerHTML = `
                        <div class="search-results-grid">
                            ${results.slice(0, 20).map(buildMovieCard).join('')}
                        </div>
                    `;
                }
            } catch {
                searchResults.innerHTML = '<div class="search-no-results">Có lỗi xảy ra. Vui lòng thử lại.</div>';
            }
        }, 300));
    }

    if (searchOverlay) {
        searchOverlay.addEventListener('click', (e) => {
            if (e.target === searchOverlay) {
                searchOverlay.classList.remove('active');
            }
        });
    }

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && searchOverlay) {
            searchOverlay.classList.remove('active');
        }
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            if (searchOverlay) {
                searchOverlay.classList.add('active');
                if (searchOverlayInput) searchOverlayInput.focus();
            }
        }
    });
}

// ==============================
// NAV BUTTONS (Scroll)
// ==============================
function initNavButtons() {
    document.querySelectorAll('.movie-row-nav').forEach(btn => {
        const targetId = btn.dataset.target;
        const target = document.getElementById(targetId);

        if (!target) return;

        btn.onclick = () => {
            const scrollAmount = target.clientWidth * 2;
            if (btn.classList.contains('nav-prev')) {
                target.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
            } else {
                target.scrollBy({ left: scrollAmount, behavior: 'smooth' });
            }
        };
    });
}

// ==============================
// HEADER SCROLL
// ==============================
function initHeaderScroll() {
    const header = document.getElementById('header');
    if (!header) return;

    window.addEventListener('scroll', () => {
        if (window.scrollY > 50) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
    });
}

// ==============================
// UTILITIES
// ==============================
function escapeHtml(str) {
    if (!str) return '';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

// ==============================
// INIT
// ==============================
function isHomePage() {
    const p = window.location.pathname;
    return p === '/' || p === '' || p.endsWith('/index.html');
}

function bootApp() {
    initHeaderScroll();
    initNavButtons();
    initSearchHandlers();
    if (isHomePage()) {
        loadHomePage();
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootApp);
} else {
    bootApp();
}
