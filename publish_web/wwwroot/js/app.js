// ==============================
// SunPhim - Netflix UI Engine
// Mobile-first, SSR-ready
// ==============================

'use strict';

// Global state
const STATE = {
    homeData: null,
    currentMovie: null,
    currentEpisodes: [],
    currentEpisodeIndex: 0,
    carouselIndex: 0,
    carouselInterval: null,
    touchStartX: 0,
    touchEndX: 0
};

// ==============================
// MOVIE CARD BUILDER
// ==============================
function buildMovieCard(movie) {
    const isNew = movie.year && movie.year >= new Date().getFullYear() - 1;
    const episodeText = movie.type === 'series' && movie.episodeCount > 0
        ? `${movie.episodeCount} tập`
        : null;
    const imgUrl = UTILS.movieThumbById(movie, 300);
    const score = movie.rating ?? movie.imdbScore;

    return `
        <div class="movie-card" data-slug="${movie.slug}" onclick="navigateToMovie('${movie.slug}')">
            <div class="movie-card-poster">
                <img src="${imgUrl}"
                     alt="${escapeHtml(movie.name)}"
                     loading="lazy"
                     referrerpolicy="no-referrer"
                     onerror="this.src='/img/placeholder.svg'; this.onerror=null;">
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
                ${score ? `
                <div class="badge-rating">
                    <svg viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
                    ${(score).toFixed(1)}
                </div>` : ''}
            </div>
        </div>
    `;
}

function buildMovieRowContent(movies) {
    if (!movies || movies.length === 0) {
        return '<div class="search-no-results" style="padding:1rem 0;">Chưa có phim nào</div>';
    }
    return movies.map(buildMovieCard).join('');
}

// ==============================
// HERO CAROUSEL
// ==============================
function initHeroCarousel(movies) {
    if (!movies || movies.length === 0) return;

    const track = document.getElementById('heroTrack');
    const indicators = document.getElementById('carouselIndicators');
    if (!track || !indicators) return;

    // Render slides
    track.innerHTML = movies.slice(0, 5).map((movie, i) => buildHeroSlide(movie, i)).join('');

    // Render indicators
    indicators.innerHTML = movies.slice(0, 5).map((_, i) =>
        `<button class="carousel-indicator ${i === 0 ? 'active' : ''}" data-index="${i}" aria-label="Slide ${i + 1}"></button>`
    ).join('');

    // Event listeners
    const prevBtn = document.getElementById('carouselPrev');
    const nextBtn = document.getElementById('carouselNext');

    prevBtn?.addEventListener('click', () => moveCarousel(-1));
    nextBtn?.addEventListener('click', () => moveCarousel(1));

    indicators.addEventListener('click', (e) => {
        const btn = e.target.closest('.carousel-indicator');
        if (btn) goToSlide(parseInt(btn.dataset.index));
    });

    // Touch/swipe support
    track.addEventListener('touchstart', (e) => {
        STATE.touchStartX = e.changedTouches[0].screenX;
    }, { passive: true });

    track.addEventListener('touchend', (e) => {
        STATE.touchEndX = e.changedTouches[0].screenX;
        const diff = STATE.touchStartX - STATE.touchEndX;
        if (Math.abs(diff) > 50) {
            if (diff > 0) moveCarousel(1);
            else moveCarousel(-1);
        }
    }, { passive: true });

    // Keyboard navigation
    document.addEventListener('keydown', (e) => {
        if (document.getElementById('heroCarousel')?.closest('[hidden]')) return;
        if (e.key === 'ArrowLeft') moveCarousel(-1);
        if (e.key === 'ArrowRight') moveCarousel(1);
    });

    // Auto-advance every 6 seconds
    STATE.carouselInterval = setInterval(() => moveCarousel(1), 6000);

    // Pause on hover/focus
    track.addEventListener('mouseenter', () => clearInterval(STATE.carouselInterval));
    track.addEventListener('mouseleave', () => {
        STATE.carouselInterval = setInterval(() => moveCarousel(1), 6000);
    });
}

function buildHeroSlide(movie, index) {
    const bgUrl = UTILS.moviePosterById(movie, 1920);
    const score = movie.rating ?? movie.imdbScore;
    const episodeText = movie.type === 'series' && movie.episodeCount > 0 ? `${movie.episodeCount} tập` : '';
    const tagsHtml = movie.categories?.slice(0, 3).map(c => `<span class="tag">${escapeHtml(c)}</span>`).join('') || '';

    return `
        <div class="hero-slide" data-index="${index}">
            <div class="hero-slide-bg" style="background-image:url('${bgUrl}')"></div>
            <div class="hero-slide-content">
                <h1 class="hero-slide-title">${escapeHtml(movie.name)}</h1>
                <div class="hero-slide-meta">
                    ${score ? `<span class="rating-badge"><svg viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>${score.toFixed(1)}</span>` : ''}
                    ${movie.year ? `<span>${movie.year}</span>` : ''}
                    ${movie.quality ? `<span>${movie.quality}</span>` : ''}
                    ${movie.lang ? `<span>${movie.lang}</span>` : ''}
                    ${episodeText ? `<span>${episodeText}</span>` : ''}
                </div>
                <p class="hero-slide-desc">${escapeHtml(movie.description || `Xem phim ${movie.name} online chất lượng cao, vietsub, thuyết minh.`)}</p>
                <div class="hero-slide-tags">${tagsHtml}</div>
                <div class="hero-slide-actions">
                    <a href="/movie.html?slug=${movie.slug}" class="btn btn-primary">
                        <svg viewBox="0 0 24 24" fill="currentColor"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                        Phát ngay
                    </a>
                    <a href="/movie.html?slug=${movie.slug}" class="btn btn-secondary">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/></svg>
                        Chi tiết
                    </a>
                </div>
            </div>
        </div>
    `;
}

function moveCarousel(direction) {
    const slides = document.querySelectorAll('.hero-slide');
    if (!slides.length) return;

    clearInterval(STATE.carouselInterval);
    STATE.carouselIndex = (STATE.carouselIndex + direction + slides.length) % slides.length;
    applyCarouselTransform();
    updateIndicators();
    STATE.carouselInterval = setInterval(() => moveCarousel(1), 6000);
}

function goToSlide(index) {
    clearInterval(STATE.carouselInterval);
    STATE.carouselIndex = index;
    applyCarouselTransform();
    updateIndicators();
    STATE.carouselInterval = setInterval(() => moveCarousel(1), 6000);
}

function applyCarouselTransform() {
    const track = document.getElementById('heroTrack');
    if (track) track.style.transform = `translateX(-${STATE.carouselIndex * 100}%)`;
}

function updateIndicators() {
    const indicators = document.getElementById('carouselIndicators');
    if (!indicators) return;
    indicators.querySelectorAll('.carousel-indicator').forEach((ind, i) => {
        ind.classList.toggle('active', i === STATE.carouselIndex);
    });
}

/** index mới: heroTrack + carousel. index cũ: heroBackdrop + heroSection */
function setupHomeHero(data) {
    const track = document.getElementById('heroTrack');
    const indicators = document.getElementById('carouselIndicators');
    const legacyBackdrop = document.getElementById('heroBackdrop');

    const carouselMovies = data.featured?.length > 0 ? data.featured
        : data.trending?.length > 0 ? data.trending
            : (data.newReleases?.slice(0, 5) || []);
    const heroOne = carouselMovies[0]
        ?? data.featured?.[0] ?? data.trending?.[0] ?? data.newReleases?.[0] ?? null;

    if (track && indicators) {
        initHeroCarousel(carouselMovies);
        return;
    }

    if (legacyBackdrop) {
        renderLegacyHero(heroOne);
    }
}

/** Tương thích index cũ khi chỉ có heroBackdrop */
function renderLegacyHero(movie) {
    const section = document.getElementById('heroSection');
    const backdrop = document.getElementById('heroBackdrop');
    if (!backdrop) return;

    if (!movie) {
        if (section) section.style.display = 'none';
        return;
    }

    if (section) section.style.display = '';

    const bgUrl = UTILS.moviePosterById(movie, 1920);
    backdrop.style.backgroundImage = `url('${bgUrl}')`;

    const titleEl = document.getElementById('heroTitle');
    if (titleEl) titleEl.textContent = movie.name;

    const score = movie.rating ?? movie.imdbScore;
    const metaHtml = [
        score ? `<span class="hero-rating"><svg viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>${Number(score).toFixed(1)}</span>` : '',
        movie.year ? `<span>${movie.year}</span>` : '',
        movie.quality ? `<span>${movie.quality}</span>` : '',
        movie.lang ? `<span>${movie.lang}</span>` : '',
        movie.episodeCount > 0 ? `<span>${movie.episodeCount} tập</span>` : ''
    ].filter(Boolean).join('<span style="opacity:0.5">|</span>');

    const metaEl = document.getElementById('heroMeta');
    if (metaEl) metaEl.innerHTML = metaHtml;

    const descEl = document.getElementById('heroDescription');
    if (descEl) {
        descEl.textContent = movie.description
            ? UTILS.truncate(movie.description, 200)
            : `Xem phim ${movie.name} online chất lượng cao, vietsub, thuyết minh.`;
    }

    const tagsEl = document.getElementById('heroTags');
    if (tagsEl) {
        tagsEl.innerHTML = movie.categories?.slice(0, 4).map(c =>
            `<span class="tag">${escapeHtml(c)}</span>`).join('') || '';
    }

    const playBtn = document.getElementById('heroPlayBtn');
    if (playBtn) playBtn.onclick = () => { window.location.href = `/movie.html?slug=${movie.slug}`; };

    const infoBtn = document.getElementById('heroInfoBtn');
    if (infoBtn) infoBtn.onclick = () => { window.location.href = `/movie.html?slug=${movie.slug}`; };

    document.title = `${movie.name} - SunPhim`;
}

function safeSetHtml(id, html) {
    const el = document.getElementById(id);
    if (el) el.innerHTML = html;
}

// ==============================
// HOMEPAGE LOADER
// ==============================
async function loadHomePage() {
    try {
        const data = await API.home.getHomePage();
        STATE.homeData = data;

        setupHomeHero(data);

        safeSetHtml('trending', buildMovieRowContent(data.trending));
        safeSetHtml('newReleases', buildMovieRowContent(data.newReleases));
        safeSetHtml('topRated', buildMovieRowContent(data.topRated));
        safeSetHtml('series', buildMovieRowContent(data.seriesList));
        safeSetHtml('singles', buildMovieRowContent(data.singleMovies));

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
            safeSetHtml('categorySections', catHtml);
        }

        initNavButtons();
        initSearchHandlers();
    } catch (error) {
        console.error('[SunPhim] Load home page failed:', error);
        UTILS.showToast('Không thể tải dữ liệu từ máy chủ.', 'error');
    }
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

        // SEO
        document.title = `${movie.name} - SunPhim`;
        document.getElementById('metaDescription')?.setAttribute('content', movie.metaDescription || movie.description || '');
        document.getElementById('ogTitle')?.setAttribute('content', movie.ogTitle || movie.name);
        document.getElementById('ogDescription')?.setAttribute('content', movie.ogDescription || movie.description || '');
        const ogImg = document.getElementById('ogImage');
        if (ogImg) ogImg.setAttribute('content', movie.ogImage
            ? UTILS.buildImageUrl(movie.ogImage, 1200)
            : UTILS.moviePosterById(movie, 1200));
        document.getElementById('schemaMarkup').textContent = movie.schemaMarkup || '';

        // Hero background
        const backdropUrl = UTILS.moviePosterById(movie, 1920);
        document.getElementById('detailHero').style.backgroundImage = `url('${backdropUrl}')`;

        // Poster
        const posterImg = document.getElementById('detailPoster');
        if (posterImg) {
            posterImg.src = UTILS.moviePosterById(movie, 800);
            posterImg.alt = movie.name;
            posterImg.referrerpolicy = 'no-referrer';
            posterImg.onerror = function() { this.src = '/img/placeholder.svg'; };
        }

        // Title
        document.getElementById('detailTitle').textContent = movie.name;
        document.getElementById('detailOriginName').textContent = movie.originName || '';

        // Meta
        const score = movie.rating ?? movie.imdbScore;
        const metaHtml = [
            score ? `<span class="detail-rating"><svg viewBox="0 0 24 24" fill="currentColor" width="14" height="14"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>${score.toFixed(1)} / 10</span>` : '',
            movie.year ? `<span class="detail-meta-item">📅 ${movie.year}</span>` : '',
            movie.duration ? `<span class="detail-meta-item">⏱️ ${movie.duration} phút</span>` : '',
            movie.quality ? `<span class="detail-meta-item">📺 ${movie.quality}</span>` : '',
            movie.lang ? `<span class="detail-meta-item">🔊 ${movie.lang}</span>` : '',
            movie.viewCount ? `<span class="detail-meta-item">👁️ ${UTILS.formatNumber(movie.viewCount)} lượt xem</span>` : ''
        ].filter(Boolean).join('');
        document.getElementById('detailMeta').innerHTML = metaHtml;

        // Tags
        const tagsHtml = movie.categories?.map(c =>
            `<a href="/the-loai/${c.toLowerCase().replace(/\s+/g, '-')}" class="tag">${escapeHtml(c)}</a>`
        ).join('') || '';
        document.getElementById('detailTags').innerHTML = tagsHtml;

        // Actions
        const firstEpisode = STATE.currentEpisodes[0];
        const playUrl = firstEpisode ? `/watch.html?movie=${slug}&episode=${firstEpisode.id}` : '#';
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

    grid.innerHTML = movie.episodes.map((ep) => {
        const thumbUrl = UTILS.movieThumbById(movie, 200);
        return `
            <div class="episode-card" data-episode-id="${ep.id}" onclick="playEpisode('${slug}', ${ep.id})">
                <div class="episode-thumb">
                    <img src="${thumbUrl}" alt="${escapeHtml(ep.name)}" loading="lazy" referrerpolicy="no-referrer" onerror="this.style.background='var(--bg-tertiary)'">
                </div>
                <div class="episode-info">
                    <p class="episode-name">${escapeHtml(ep.name)}</p>
                    <p class="episode-number">Tập ${ep.episodeNumber}</p>
                </div>
            </div>
        `;
    }).join('');
}

function playEpisode(movieSlug, episodeId) {
    window.location.href = `/watch.html?movie=${movieSlug}&episode=${episodeId}`;
}

// ==============================
// EPISODE PLAYER LOADER
// ==============================
async function loadEpisodePlayer(movieSlug, episodeId) {
    try {
        document.getElementById('playerTitle').textContent = 'Đang tải player...';

        const movie = await API.movies.getBySlug(movieSlug);
        if (!movie) { showPlayerError('Không tìm thấy phim'); return; }

        STATE.currentMovie = movie;
        STATE.currentEpisodes = movie.episodes || [];

        const episode = STATE.currentEpisodes.find(e => e.id === episodeId);
        if (!episode) { showPlayerError('Không tìm thấy tập phim'); return; }

        const epIndex = STATE.currentEpisodes.indexOf(episode);
        STATE.currentEpisodeIndex = epIndex;

        document.getElementById('playerTitle').textContent = `${movie.name} - ${episode.name}`;

        // Episode mini list
        const miniHtml = STATE.currentEpisodes.map((ep, idx) => {
            const thumbUrl = UTILS.movieThumbById(movie, 200);
            return `
            <div class="episode-card ${idx === epIndex ? 'active' : ''}" onclick="playEpisodeFromPlayer('${movieSlug}', ${ep.id}, ${idx})">
                <div class="episode-thumb">
                    <img src="${thumbUrl}" alt="${escapeHtml(ep.name)}" loading="lazy" referrerpolicy="no-referrer">
                </div>
                <div class="episode-info">
                    <p class="episode-name">${escapeHtml(ep.name)}</p>
                    <p class="episode-number">Tập ${ep.episodeNumber}</p>
                </div>
            </div>
        `;
        }).join('');

        document.getElementById('episodesMiniList').innerHTML = `
            <h3 style="margin:1.5rem 0 1rem;font-size:1.1rem;font-weight:700;">Danh sách tập</h3>
            <div class="episodes-grid">${miniHtml}</div>
        `;

        // Nav buttons
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

        await loadPlayer(episode);
        document.title = `${movie.name} - ${episode.name} - SunPhim`;
    } catch (error) {
        console.error('Load player failed:', error);
        showPlayerError('Không thể tải video. Vui lòng thử lại.');
    }
}

async function loadPlayer(episode) {
    const wrapper = document.getElementById('playerWrapper');

    if (episode.embedLink) {
        wrapper.innerHTML = `
            <iframe src="${episode.embedLink}" title="Player" allowfullscreen
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share; fullscreen"
                referrerpolicy="strict-origin-when-cross-origin" loading="lazy"></iframe>`;
    } else if (episode.fileUrl) {
        try {
            const playerData = await API.streaming.getPlayerUrl(episode.id);
            if (playerData.playerUrl) {
                wrapper.innerHTML = `
                    <iframe src="${playerData.playerUrl}" allowfullscreen allow="autoplay; fullscreen"
                        referrerpolicy="strict-origin-when-cross-origin"></iframe>`;
            } else if (episode.fileUrl.includes('.m3u8')) {
                wrapper.innerHTML = `<video id="hlsVideo" controls playsinline><source src="${episode.fileUrl}" type="application/x-mpegURL"></video>`;
                initHlsPlayer(episode.fileUrl);
            } else {
                wrapper.innerHTML = `<video id="directVideo" controls playsinline autoplay><source src="${episode.fileUrl}" type="video/mp4"></video>`;
            }
        } catch {
            if (episode.fileUrl.includes('.m3u8')) {
                wrapper.innerHTML = `<video id="hlsVideo" controls playsinline><source src="${episode.fileUrl}" type="application/x-mpegURL"></video>`;
                initHlsPlayer(episode.fileUrl);
            } else {
                wrapper.innerHTML = `<video id="directVideo" controls playsinline autoplay><source src="${episode.fileUrl}" type="video/mp4"></video>`;
            }
        }
    } else {
        showPlayerError('Chưa có nguồn phát cho tập này');
    }
}

function initHlsPlayer(url) {
    const videoEl = document.getElementById('hlsVideo');
    if (!videoEl || typeof Hls === 'undefined') return;
    if (Hls.isSupported()) { const hls = new Hls(); hls.loadSource(url); hls.attachMedia(videoEl); }
    else if (videoEl.canPlayType('application/vnd.apple.mpegurl')) { videoEl.src = url; }
}

function showPlayerError(message) {
    document.getElementById('playerWrapper').innerHTML = `
        <div class="player-error">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>
            </svg>
            <p>${escapeHtml(message)}</p>
        </div>`;
}

function playEpisodeFromPlayer(movieSlug, episodeId) {
    window.location.href = `/watch.html?movie=${movieSlug}&episode=${episodeId}`;
}

// ==============================
// NAVIGATION
// ==============================
function navigateToMovie(slug) { window.location.href = `/movie.html?slug=${slug}`; }

// ==============================
// SEARCH
// ==============================
function initSearchHandlers() {
    const searchInput = document.getElementById('searchInput');
    const searchBtn = document.getElementById('searchBtn');
    const searchOverlay = document.getElementById('searchOverlay');
    const searchOverlayInput = document.getElementById('searchOverlayInput');
    const searchResults = document.getElementById('searchResults');
    const searchOverlayClose = document.getElementById('searchOverlayClose');

    if (searchBtn) searchBtn.onclick = () => { searchOverlay?.classList.add('active'); searchOverlayInput?.focus(); };

    searchOverlayClose?.addEventListener('click', () => searchOverlay?.classList.remove('active'));

    if (searchOverlayInput) {
        searchOverlayInput.addEventListener('input', UTILS.debounce(async (e) => {
            const keyword = e.target.value.trim();
            if (keyword.length < 2) { searchResults.innerHTML = '<div class="search-no-results">Nhập ít nhất 2 ký tự để tìm kiếm</div>'; return; }

            try {
                const results = await API.movies.search(keyword);
                if (!results || results.length === 0) {
                    searchResults.innerHTML = '<div class="search-no-results">Không tìm thấy kết quả nào</div>';
                } else {
                    searchResults.innerHTML = `<div class="search-results-grid">${results.slice(0, 20).map(buildMovieCard).join('')}</div>`;
                }
            } catch { searchResults.innerHTML = '<div class="search-no-results">Có lỗi xảy ra. Vui lòng thử lại.</div>'; }
        }, 300));
    }

    searchOverlay?.addEventListener('click', (e) => { if (e.target === searchOverlay) searchOverlay.classList.remove('active'); });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && searchOverlay) searchOverlay.classList.remove('active');
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            searchOverlay?.classList.add('active');
            searchOverlayInput?.focus();
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
            const scrollAmount = Math.max(target.clientWidth * 1.5, 400);
            target.scrollBy({ left: btn.classList.contains('nav-prev') ? -scrollAmount : scrollAmount, behavior: 'smooth' });
        };
    });
}

// ==============================
// HEADER SCROLL
// ==============================
function initHeaderScroll() {
    const header = document.getElementById('header');
    if (!header) return;
    window.addEventListener('scroll', () => header.classList.toggle('scrolled', window.scrollY > 50), { passive: true });
}

// ==============================
// MOBILE MENU
// ==============================
function initMobileMenu() {
    const btn = document.getElementById('mobileMenuBtn');
    const nav = document.getElementById('mobileNav');
    if (!btn || !nav) return;
    btn.onclick = () => nav.classList.toggle('open');
    nav.querySelectorAll('.mobile-nav-link').forEach(link => {
        link.onclick = () => nav.classList.remove('open');
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
// ROUTING (index.html SPA)
// ==============================
function normalizePathname() {
    let p = window.location.pathname || '/';
    if (p.endsWith('/index.html')) p = '/';
    if (p.length > 1 && p.endsWith('/')) p = p.slice(0, -1);
    return p || '/';
}

function setActiveNavForPath(path) {
    const p = path || normalizePathname();
    const navLinks = document.querySelectorAll('.nav-link, .mobile-nav-link');
    navLinks.forEach(a => a.classList.remove('active'));
    if (p === '/' || p === '') {
        document.querySelector('.nav-link[data-nav="home"]')?.classList.add('active');
        document.querySelector('.mobile-nav-link[data-nav="home"]')?.classList.add('active');
        return;
    }
    if (p === '/phim-bo') { document.querySelector('.nav-link[data-nav="series"]')?.classList.add('active'); document.querySelector('.mobile-nav-link[data-nav="series"]')?.classList.add('active'); return; }
    if (p === '/phim-le') { document.querySelector('.nav-link[data-nav="single"]')?.classList.add('active'); document.querySelector('.mobile-nav-link[data-nav="single"]')?.classList.add('active'); return; }
    if (p === '/the-loai' || p.startsWith('/the-loai/')) { document.querySelector('.nav-link[data-nav="genres"]')?.classList.add('active'); document.querySelector('.mobile-nav-link[data-nav="genres"]')?.classList.add('active'); return; }
}
window.setActiveNavForPath = setActiveNavForPath;

function showHomeShell() {
    document.getElementById('homeShell').hidden = false;
    document.getElementById('browseShell').hidden = true;
}

function showBrowseShell() {
    document.getElementById('homeShell').hidden = true;
    document.getElementById('browseShell').hidden = false;
}

// ==============================
// BROWSE PAGES
// ==============================
async function loadBrowseByType(type, pageTitle) {
    showBrowseShell();
    setActiveNavForPath();
    document.getElementById('genreChipsWrap').hidden = false;
    document.getElementById('browseTitle').textContent = pageTitle;
    document.getElementById('browseSubtitle').hidden = true;
    document.getElementById('browseGrid').className = 'movie-grid-browse';
    document.title = `${pageTitle} - SunPhim`;
    await renderGenreChips(null);
    try {
        const movies = await API.movies.getAll(type);
        renderBrowseGrid(movies);
    } catch { UTILS.showToast('Không tải được danh sách phim.', 'error'); renderBrowseGrid([]); }
}

async function loadBrowseByCategory(slug) {
    showBrowseShell();
    setActiveNavForPath();
    document.getElementById('genreChipsWrap').hidden = false;
    document.getElementById('browseGrid').className = 'movie-grid-browse';
    try {
        const cat = await API.categories.getBySlug(slug);
        document.getElementById('browseTitle').textContent = cat?.name || slug.replace(/-/g, ' ');
        document.getElementById('browseSubtitle').hidden = false;
        document.getElementById('browseSubtitle').textContent = 'Phim thuộc thể loại này';
    } catch { document.getElementById('browseTitle').textContent = slug.replace(/-/g, ' '); }
    document.title = `${document.getElementById('browseTitle').textContent} - SunPhim`;
    await renderGenreChips(slug);
    try {
        const movies = await API.movies.getByCategory(slug);
        renderBrowseGrid(movies);
    } catch { UTILS.showToast('Không tải được phim theo thể loại.', 'error'); renderBrowseGrid([]); }
}

async function loadCategoryHub() {
    showBrowseShell();
    setActiveNavForPath();
    document.getElementById('genreChipsWrap').hidden = true;
    document.getElementById('browseTitle').textContent = 'Thể loại phim';
    document.getElementById('browseSubtitle').hidden = false;
    document.getElementById('browseSubtitle').textContent = 'Chọn thể loại phù hợp';
    document.getElementById('genreChips').innerHTML = '';
    document.getElementById('browseGrid').className = 'category-tiles-grid';
    document.title = 'Thể loại - SunPhim';
    try {
        const cats = await API.categories.getAll();
        if (!cats?.length) { document.getElementById('browseGrid').innerHTML = ''; document.getElementById('browseEmpty').hidden = false; return; }
        document.getElementById('browseEmpty').hidden = true;
        document.getElementById('browseGrid').innerHTML = cats.map(c =>
            `<a href="/the-loai/${encodeURIComponent(c.slug)}" class="category-tile"><span class="category-tile__name">${escapeHtml(c.name)}</span></a>`
        ).join('');
    } catch { UTILS.showToast('Không tải được danh sách thể loại.', 'error'); }
}

async function loadBrowseFromHomeSlice(fetcher, pageTitle) {
    showBrowseShell();
    setActiveNavForPath();
    document.getElementById('genreChipsWrap').hidden = false;
    document.getElementById('browseTitle').textContent = pageTitle;
    document.getElementById('browseSubtitle').hidden = true;
    document.getElementById('browseGrid').className = 'movie-grid-browse';
    document.title = `${pageTitle} - SunPhim`;
    await renderGenreChips(null);
    try { renderBrowseGrid(await fetcher()); } catch { renderBrowseGrid([]); }
}

function renderBrowseGrid(movies) {
    const grid = document.getElementById('browseGrid');
    const empty = document.getElementById('browseEmpty');
    if (!grid) return;
    if (!movies?.length) { grid.innerHTML = ''; if (empty) empty.hidden = false; return; }
    if (empty) empty.hidden = true;
    grid.innerHTML = movies.map(buildMovieCard).join('');
    initNavButtons();
}

async function renderGenreChips(activeSlug) {
    const el = document.getElementById('genreChips');
    if (!el) return;
    try {
        const cats = await API.categories.getAll();
        el.innerHTML = cats.map(c => {
            const act = activeSlug && c.slug === activeSlug ? ' genre-chip--active' : '';
            return `<a href="/the-loai/${encodeURIComponent(c.slug)}" class="genre-chip${act}">${escapeHtml(c.name)}</a>`;
        }).join('');
    } catch { el.innerHTML = ''; }
}

function routeIndexApp() {
    const p = normalizePathname();
    setActiveNavForPath();
    if (p === '/' || p === '') { showHomeShell(); loadHomePage(); return; }
    if (p === '/phim-bo') { loadBrowseByType('series', 'Phim bộ'); return; }
    if (p === '/phim-le') { loadBrowseByType('single', 'Phim lẻ'); return; }
    if (p === '/the-loai') { loadCategoryHub(); return; }
    if (p.startsWith('/the-loai/')) {
        const slug = p.slice('/the-loai/'.length).split('/').filter(Boolean)[0];
        if (slug) { loadBrowseByCategory(decodeURIComponent(slug)); return; }
        loadCategoryHub(); return;
    }
    if (p === '/phim-moi') { loadBrowseFromHomeSlice(() => API.home.getNewReleases(120), 'Phim mới cập nhật'); return; }
    if (p === '/top-rated') { loadBrowseFromHomeSlice(() => API.home.getTopRated(120), 'Điểm cao'); return; }
    if (p === '/xuhuong') { loadBrowseFromHomeSlice(() => API.home.getTrending(120), 'Xu hướng'); return; }
    showHomeShell(); loadHomePage();
}

function bootApp() {
    initHeaderScroll();
    initMobileMenu();
    initSearchHandlers();
    if (document.getElementById('homeShell')) routeIndexApp();
    else { setActiveNavForPath(); initNavButtons(); }
}

window.setActiveNavForPath = setActiveNavForPath;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootApp);
} else {
    bootApp();
}
