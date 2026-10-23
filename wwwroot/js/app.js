/**
 * SunPhim - Obsidian Cinema Application Engine
 * Ultra-Responsive, Zero-Clipping Cinema Player, Netflix-Grade Micro-Interactions
 */

'use strict';

// Global Application State
const STATE = {
    homeData: null,
    currentMovie: null,
    currentEpisodes: [],
    currentEpisodeIndex: 0,
    currentServer: 'vip1',
    aspectRatio: '16-9',
    theaterMode: false,
    lightsOff: false,
    carouselIndex: 0,
    carouselInterval: null,
    autoNextTimer: null,
    touchStartX: 0,
    touchEndX: 0
};

// =============================================================================
// MOVIE CARD BUILDER (THE OBSIDIAN CINEMA CARD)
// =============================================================================
function buildMovieCard(movie) {
    if (!movie) return '';
    const isNew = movie.year && movie.year >= new Date().getFullYear() - 1;
    const isSeries = movie.type === 'series' || movie.type === 'Series';
    const episodeText = isSeries && movie.episodeCount > 0 ? `${movie.episodeCount} tập` : null;
    const imgUrl = UTILS.movieThumbById(movie);
    const score = movie.rating ?? movie.imdbScore ?? 8.5;
    const inWatchlist = UTILS.isInWatchlist(movie.slug);

    return `
        <div class="movie-card" data-slug="${movie.slug}" onclick="handleCardClick(event, '${movie.slug}')">
            <div class="movie-card-poster">
                <img src="${imgUrl}"
                     alt="${escapeHtml(movie.name)}"
                     loading="lazy"
                     referrerpolicy="no-referrer"
                     onerror="this.src='/img/placeholder.svg'; this.onerror=null;">
                <div class="movie-card-overlay">
                    <p class="movie-card-title">${escapeHtml(movie.name)}</p>
                    <div class="movie-card-meta">
                        ${movie.quality ? `<span class="meta-pill quality">${movie.quality}</span>` : ''}
                        ${movie.lang ? `<span class="meta-pill lang">${movie.lang}</span>` : ''}
                        ${episodeText ? `<span class="meta-pill">${episodeText}</span>` : ''}
                    </div>
                </div>
                <div class="movie-card-badges">
                    ${movie.quality ? `<span class="badge-quality">${movie.quality}</span>` : ''}
                    ${isNew ? `<span class="badge-new">MỚI</span>` : ''}
                </div>
                ${score ? `
                <div class="badge-rating">
                    <svg viewBox="0 0 24 24"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
                    ${Number(score).toFixed(1)}
                </div>` : ''}
                <div class="movie-card-play-ring">
                    <svg viewBox="0 0 24 24"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                </div>
                <div class="movie-card-actions">
                    <button class="card-action-btn" title="Phát ngay" onclick="directPlayMovie(event, '${movie.slug}')">
                        <svg viewBox="0 0 24 24" fill="currentColor"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                    </button>
                    <button class="card-action-btn ${inWatchlist ? 'active' : ''}" title="${inWatchlist ? 'Xóa khỏi danh sách' : 'Lưu vào danh sách'}" onclick="toggleWatchlistCard(event, '${movie.slug}')">
                        <svg viewBox="0 0 24 24" fill="${inWatchlist ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg>
                    </button>
                    <button class="card-action-btn" title="Xem nhanh thông tin" onclick="openQuickModal(event, '${movie.slug}')">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"/></svg>
                    </button>
                </div>
            </div>
        </div>
    `;
}

function handleCardClick(e, slug) {
    if (e.target.closest('.card-action-btn')) return;
    window.location.href = `/movie.html?slug=${slug}`;
}

function directPlayMovie(e, slug) {
    e.stopPropagation();
    window.location.href = `/watch.html?movie=${slug}&episode=1`;
}

function toggleWatchlistCard(e, slug) {
    e.stopPropagation();
    const movie = findMovieInState(slug);
    if (movie) {
        UTILS.toggleWatchlist(movie);
        renderWatchlistSection();
        // Update button visual
        const btn = e.currentTarget;
        const inList = UTILS.isInWatchlist(slug);
        btn.classList.toggle('active', inList);
        btn.querySelector('svg').setAttribute('fill', inList ? 'currentColor' : 'none');
    }
}

function findMovieInState(slug) {
    if (!slug) return null;
    if (STATE.currentMovie && STATE.currentMovie.slug === slug) return STATE.currentMovie;
    if (STATE.homeData) {
        const pool = [
            ...(STATE.homeData.featured || []),
            ...(STATE.homeData.trending || []),
            ...(STATE.homeData.newReleases || []),
            ...(STATE.homeData.topRated || []),
            ...(STATE.homeData.seriesList || []),
            ...(STATE.homeData.singleMovies || [])
        ];
        return pool.find(m => m.slug === slug) || null;
    }
    return null;
}

// =============================================================================
// HERO BILLBOARD CAROUSEL
// =============================================================================
function initHeroCarousel(movies) {
    if (!movies || movies.length === 0) return;

    const track = document.getElementById('heroTrack');
    const indicators = document.getElementById('carouselIndicators');
    if (!track || !indicators) return;

    const featuredList = movies.slice(0, 6);

    // Render slides
    track.innerHTML = featuredList.map((movie, i) => buildHeroSlide(movie, i)).join('');

    // Render indicators
    indicators.innerHTML = featuredList.map((_, i) =>
        `<div class="carousel-indicator ${i === 0 ? 'active' : ''}" data-index="${i}" role="button" aria-label="Slide ${i + 1}"></div>`
    ).join('');

    // Carousel buttons
    const prevBtn = document.getElementById('carouselPrev');
    const nextBtn = document.getElementById('carouselNext');

    prevBtn?.addEventListener('click', () => moveCarousel(-1));
    nextBtn?.addEventListener('click', () => moveCarousel(1));

    indicators.addEventListener('click', (e) => {
        const ind = e.target.closest('.carousel-indicator');
        if (ind) goToSlide(parseInt(ind.dataset.index, 10));
    });

    // Touch gesture swipe support
    track.addEventListener('touchstart', (e) => {
        STATE.touchStartX = e.changedTouches[0].screenX;
    }, { passive: true });

    track.addEventListener('touchend', (e) => {
        STATE.touchEndX = e.changedTouches[0].screenX;
        const diff = STATE.touchStartX - STATE.touchEndX;
        if (Math.abs(diff) > 40) {
            if (diff > 0) moveCarousel(1);
            else moveCarousel(-1);
        }
    }, { passive: true });

    // Autoplay
    startCarouselAutoPlay();
    track.addEventListener('mouseenter', stopCarouselAutoPlay);
    track.addEventListener('mouseleave', startCarouselAutoPlay);
}

function buildHeroSlide(movie, index) {
    const bgUrl = UTILS.moviePosterById(movie);
    const score = movie.rating ?? movie.imdbScore ?? 8.6;
    const isSeries = movie.type === 'series' || movie.type === 'Series';
    const episodeText = isSeries && movie.episodeCount > 0 ? `${movie.episodeCount} tập` : '';
    const tagsHtml = movie.categories?.slice(0, 3).map(c => `<span class="tag">${escapeHtml(c)}</span>`).join('') || '';

    return `
        <div class="hero-slide ${index === 0 ? 'active' : ''}" data-index="${index}">
            <div class="hero-slide-bg" style="background-image:url('${bgUrl}')"></div>
            <div class="hero-slide-content">
                <span class="hero-badge-pill">
                    <svg viewBox="0 0 24 24" fill="currentColor" width="13" height="13"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
                    Đặc Sắc SunPhim #${index + 1}
                </span>
                <h1 class="hero-slide-title">${escapeHtml(movie.name)}</h1>
                <div class="hero-slide-meta">
                    <span class="rating-badge">
                        <svg viewBox="0 0 24 24"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
                        ${Number(score).toFixed(1)} IMDb
                    </span>
                    <span class="meta-pill quality">${movie.quality || '4K Ultra HD'}</span>
                    <span class="meta-pill lang">${movie.lang || 'Vietsub'}</span>
                    ${movie.year ? `<span class="meta-pill">${movie.year}</span>` : ''}
                    ${episodeText ? `<span class="meta-pill">${episodeText}</span>` : ''}
                </div>
                <p class="hero-slide-desc">${escapeHtml(movie.description || `Xem ngay siêu phẩm điện ảnh ${movie.name} chất lượng cao 4K trên SunPhim.`)}</p>
                <div class="hero-slide-tags">${tagsHtml}</div>
                <div class="hero-slide-actions">
                    <a href="/watch.html?movie=${movie.slug}&episode=1" class="btn btn-primary">
                        <svg viewBox="0 0 24 24" fill="currentColor"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                        Phát ngay
                    </a>
                    <button class="btn btn-secondary" onclick="openQuickModal(event, '${movie.slug}')">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/></svg>
                        Chi tiết phim
                    </button>
                    <button class="btn btn-icon" title="Lưu danh sách" onclick="toggleWatchlistCard(event, '${movie.slug}')">
                        <svg viewBox="0 0 24 24" fill="${UTILS.isInWatchlist(movie.slug) ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg>
                    </button>
                </div>
            </div>
        </div>
    `;
}

function startCarouselAutoPlay() {
    stopCarouselAutoPlay();
    STATE.carouselInterval = setInterval(() => moveCarousel(1), 6500);
}

function stopCarouselAutoPlay() {
    if (STATE.carouselInterval) clearInterval(STATE.carouselInterval);
}

function moveCarousel(direction) {
    const slides = document.querySelectorAll('.hero-slide');
    if (!slides.length) return;
    STATE.carouselIndex = (STATE.carouselIndex + direction + slides.length) % slides.length;
    applyCarouselSlide();
}

function goToSlide(index) {
    STATE.carouselIndex = index;
    applyCarouselSlide();
}

function applyCarouselSlide() {
    const track = document.getElementById('heroTrack');
    const slides = document.querySelectorAll('.hero-slide');
    const indicators = document.querySelectorAll('.carousel-indicator');

    if (track) track.style.transform = `translateX(-${STATE.carouselIndex * 100}%)`;
    slides.forEach((s, idx) => s.classList.toggle('active', idx === STATE.carouselIndex));
    indicators.forEach((ind, idx) => ind.classList.toggle('active', idx === STATE.carouselIndex));
}

// =============================================================================
// CONTINUE WATCHING & WATCHLIST SECTIONS
// =============================================================================
function renderContinueWatchingSection() {
    const section = document.getElementById('continueWatchingSection');
    const container = document.getElementById('continueWatching');
    if (!section || !container) return;

    const list = UTILS.getContinueWatchingList();
    if (list.length === 0) {
        section.style.display = 'none';
        return;
    }

    section.style.display = 'block';
    container.innerHTML = list.map(item => `
        <div class="continue-card" onclick="window.location.href='/watch.html?movie=${item.slug}&episode=${item.episodeId}'">
            <div class="continue-thumb-wrap">
                <img src="${item.thumb || item.poster || '/img/placeholder.svg'}" alt="${escapeHtml(item.name)}" referrerpolicy="no-referrer">
                <div class="continue-progress-bar">
                    <div class="continue-progress-fill" style="width:${item.progress || 50}%;"></div>
                </div>
                <button class="continue-delete-btn" title="Xóa khỏi lịch sử" onclick="event.stopPropagation(); UTILS.removeContinueWatching('${item.slug}'); renderContinueWatchingSection();">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="14" height="14"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </div>
            <div class="continue-info">
                <div class="continue-title">${escapeHtml(item.name)}</div>
                <div class="continue-subtext">
                    <span>${escapeHtml(item.episodeName || 'Tập 1')}</span>
                    <span style="color:var(--accent-cherry); font-weight:700;">${item.progress}%</span>
                </div>
            </div>
        </div>
    `).join('');
}

function renderWatchlistSection() {
    const section = document.getElementById('myWatchlistSection');
    const container = document.getElementById('myWatchlistRow');
    if (!section || !container) return;

    const list = UTILS.getWatchlist();
    if (list.length === 0) {
        section.style.display = 'none';
        return;
    }

    section.style.display = 'block';
    container.innerHTML = list.map(buildMovieCard).join('');
}

// =============================================================================
// HOMEPAGE INITIALIZATION
// =============================================================================
async function loadHomePage() {
    try {
        const data = await API.home.getHomePage();
        STATE.homeData = data;

        // Init Hero Carousel with featured movies
        const heroMovies = data.featured?.length > 0 ? data.featured
            : data.trending?.length > 0 ? data.trending
            : (data.newReleases || []);
        initHeroCarousel(heroMovies);

        // Render Continue Watching & Watchlist
        renderContinueWatchingSection();
        renderWatchlistSection();

        // Render standard rows
        renderRowContent('trending', data.trending);
        renderRowContent('newReleases', data.newReleases);
        renderRowContent('topRated', data.topRated);
        renderRowContent('series', data.seriesList);
        renderRowContent('singles', data.singleMovies);

        // Render Category Rows if available
        if (data.categories?.length > 0) {
            const catHtml = data.categories.map(cat => `
                <section class="section">
                    <div class="section-header">
                        <h2 class="section-title">${escapeHtml(cat.name)}</h2>
                        <a href="/the-loai/${encodeURIComponent(cat.slug)}" class="section-link">
                            Xem tất cả
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"/></svg>
                        </a>
                    </div>
                    <div class="movie-row">
                        <button class="movie-row-nav nav-prev" data-target="cat-${cat.id}" aria-label="Lùi">‹</button>
                        <div class="movie-row-inner" id="cat-${cat.id}">${(cat.movies || []).map(buildMovieCard).join('')}</div>
                        <button class="movie-row-nav nav-next" data-target="cat-${cat.id}" aria-label="Tiến">›</button>
                    </div>
                </section>
            `).join('');
            const catEl = document.getElementById('categorySections');
            if (catEl) catEl.innerHTML = catHtml;
        }

        initRowNavControls();
        initFastGenreFilters();
    } catch (error) {
        console.error('Load home page failed:', error);
        UTILS.showToast('Không thể kết nối máy chủ dữ liệu phim.', 'error');
    }
}

function renderRowContent(containerId, movies) {
    const el = document.getElementById(containerId);
    if (!el) return;
    if (!movies || movies.length === 0) {
        el.innerHTML = '<div style="color:var(--text-muted); padding:1rem 0;">Đang cập nhật phim...</div>';
        return;
    }
    el.innerHTML = movies.map(buildMovieCard).join('');
}

// Fast Genre Filter Pills on Homepage
function initFastGenreFilters() {
    const container = document.getElementById('fastGenreFilter');
    if (!container) return;

    container.addEventListener('click', (e) => {
        const btn = e.target.closest('.genre-filter-pill');
        if (!btn) return;

        container.querySelectorAll('.genre-filter-pill').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');

        const genre = btn.dataset.genre;
        if (genre === 'all') {
            loadHomePage();
        } else {
            loadBrowseByCategory(genre);
        }
    });
}

// Horizontal Row Navigation Controls
function initRowNavControls() {
    document.querySelectorAll('.movie-row-nav').forEach(btn => {
        const targetId = btn.dataset.target;
        const target = document.getElementById(targetId);
        if (!target) return;

        btn.onclick = () => {
            const scrollAmount = Math.max(target.clientWidth * 0.8, 350);
            const isPrev = btn.classList.contains('nav-prev');
            target.scrollBy({ left: isPrev ? -scrollAmount : scrollAmount, behavior: 'smooth' });
        };
    });
}

// =============================================================================
// NETFLIX QUICK INFO MODAL (XEM NHANH CHI TIẾT)
// =============================================================================
async function openQuickModal(e, slug) {
    if (e) e.stopPropagation();

    const backdrop = document.getElementById('quickModalBackdrop');
    if (!backdrop) return;

    // Reset modal content
    document.getElementById('quickModalTitle').textContent = 'Đang tải thông tin...';
    document.getElementById('quickModalDesc').textContent = 'Đang chuẩn bị nội dung chi tiết...';
    document.getElementById('quickModalEpisodesGrid').innerHTML = '';

    backdrop.classList.add('open');
    document.body.style.overflow = 'hidden';

    try {
        let movie = findMovieInState(slug);
        if (!movie || !movie.episodes) {
            movie = await API.movies.getBySlug(slug);
        }

        if (!movie) {
            UTILS.showToast('Không tìm thấy thông tin phim', 'error');
            closeQuickModal();
            return;
        }

        document.getElementById('quickModalTitle').textContent = movie.name;
        document.getElementById('quickModalDesc').textContent = movie.description || 'Xem phim online chất lượng cao 4K trên SunPhim.';

        // Background hero
        const bgUrl = UTILS.moviePosterById(movie);
        document.getElementById('quickModalHero').style.backgroundImage = `url('${bgUrl}')`;

        // Play button
        const playBtn = document.getElementById('quickModalPlayBtn');
        playBtn.href = `/watch.html?movie=${movie.slug}&episode=1`;

        // Watchlist button
        const watchlistBtn = document.getElementById('quickModalWatchlistBtn');
        const inList = UTILS.isInWatchlist(movie.slug);
        watchlistBtn.innerHTML = inList
            ? `<svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Đã lưu`
            : `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Thêm vào danh sách`;

        watchlistBtn.onclick = () => {
            UTILS.toggleWatchlist(movie);
            const nowInList = UTILS.isInWatchlist(movie.slug);
            watchlistBtn.innerHTML = nowInList
                ? `<svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Đã lưu`
                : `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Thêm vào danh sách`;
            renderWatchlistSection();
        };

        // Meta tags
        const score = movie.rating ?? movie.imdbScore ?? 8.5;
        document.getElementById('quickModalMeta').innerHTML = `
            <span class="rating-badge">
                <svg viewBox="0 0 24 24"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>
                ${Number(score).toFixed(1)} IMDb
            </span>
            <span class="meta-pill quality">${movie.quality || '4K Ultra HD'}</span>
            <span class="meta-pill lang">${movie.lang || 'Vietsub'}</span>
            ${movie.year ? `<span class="meta-pill">${movie.year}</span>` : ''}
        `;

        // Tags
        document.getElementById('quickModalTags').innerHTML = (movie.categories || ['Điện Ảnh']).map(c => `
            <span class="tag">${escapeHtml(c)}</span>
        `).join('');

        // Episodes Grid
        const episodes = movie.episodes || [];
        const epGrid = document.getElementById('quickModalEpisodesGrid');
        if (episodes.length > 0) {
            epGrid.innerHTML = episodes.map((ep, idx) => `
                <a href="/watch.html?movie=${movie.slug}&episode=${ep.id || idx + 1}" class="quick-ep-btn">
                    ${escapeHtml(ep.name || `Tập ${idx + 1}`)}
                </a>
            `).join('');
            document.getElementById('quickModalEpisodesWrap').style.display = 'block';
        } else {
            document.getElementById('quickModalEpisodesWrap').style.display = 'none';
        }

    } catch (err) {
        console.error('Quick modal error:', err);
    }
}

function closeQuickModal() {
    const backdrop = document.getElementById('quickModalBackdrop');
    if (backdrop) backdrop.classList.remove('open');
    document.body.style.overflow = '';
}

// Modal Click & Key Listeners
document.getElementById('quickModalClose')?.addEventListener('click', closeQuickModal);
document.getElementById('quickModalBackdrop')?.addEventListener('click', (e) => {
    if (e.target.id === 'quickModalBackdrop') closeQuickModal();
});

// =============================================================================
// SPOTLIGHT SEARCH (CTRL+K)
// =============================================================================
function initSpotlightSearch() {
    const overlay = document.getElementById('searchOverlay');
    const input = document.getElementById('searchOverlayInput');
    const resultsContainer = document.getElementById('searchResults');
    const closeBtn = document.getElementById('searchOverlayClose');
    const headerTrigger = document.getElementById('headerSearchTrigger');
    const mobileDockBtn = document.getElementById('mobileSearchDockBtn');

    function openSearch() {
        if (!overlay) return;
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
        setTimeout(() => input?.focus(), 50);
    }

    function closeSearch() {
        if (!overlay) return;
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    headerTrigger?.addEventListener('click', openSearch);
    mobileDockBtn?.addEventListener('click', openSearch);
    closeBtn?.addEventListener('click', closeSearch);
    overlay?.addEventListener('click', (e) => {
        if (e.target === overlay) closeSearch();
    });

    // Trending keyword pills inside search modal
    document.querySelectorAll('.trending-tag-pill').forEach(pill => {
        pill.addEventListener('click', () => {
            const kw = pill.dataset.kw;
            if (input) {
                input.value = kw;
                input.dispatchEvent(new Event('input'));
            }
        });
    });

    // Live search input
    input?.addEventListener('input', UTILS.debounce(async (e) => {
        const query = e.target.value.trim();
        if (query.length < 2) {
            resultsContainer.innerHTML = `
                <div>
                    <span style="color:var(--text-muted); font-size:0.85rem; font-weight:700;">Từ khóa gợi ý thịnh hành:</span>
                    <div class="search-trending-tags">
                        <button class="trending-tag-pill" data-kw="Dune">Dune 2</button>
                        <button class="trending-tag-pill" data-kw="Squid Game">Squid Game</button>
                        <button class="trending-tag-pill" data-kw="Mai">Mai (2024)</button>
                        <button class="trending-tag-pill" data-kw="Conan">Conan</button>
                        <button class="trending-tag-pill" data-kw="Lật Mặt">Lật Mặt 7</button>
                    </div>
                </div>`;
            return;
        }

        try {
            resultsContainer.innerHTML = '<div class="search-no-results">Đang tìm kiếm phim...</div>';
            const results = await API.movies.search(query);
            if (!results || results.length === 0) {
                resultsContainer.innerHTML = `<div class="search-no-results">Không tìm thấy phim nào khớp với "${escapeHtml(query)}"</div>`;
                return;
            }

            resultsContainer.innerHTML = `
                <div class="search-results-grid">
                    ${results.slice(0, 18).map(buildMovieCard).join('')}
                </div>
            `;
        } catch {
            resultsContainer.innerHTML = '<div class="search-no-results">Lỗi kết nối khi tìm kiếm. Vui lòng thử lại.</div>';
        }
    }, 280));
}

// =============================================================================
// WATCH PAGE CINEMA ENGINE (ZERO-CLIPPING CINEMA PLAYER)
// =============================================================================
async function initWatchPageEngine(movieSlug, episodeId) {
    try {
        const titleEl = document.getElementById('playerTitle');
        if (titleEl) titleEl.textContent = 'Đang tải thông tin phim...';

        const movie = await API.movies.getBySlug(movieSlug);
        if (!movie) {
            showPlayerError('Không tìm thấy dữ liệu bộ phim này.');
            return;
        }

        STATE.currentMovie = movie;
        STATE.currentEpisodes = movie.episodes || [];

        if (STATE.currentEpisodes.length === 0) {
            showPlayerError('Phim này đang được cập nhật tập mới.');
            return;
        }

        // Find matching episode
        const episode = STATE.currentEpisodes.find(e => String(e.id) === String(episodeId))
                     || STATE.currentEpisodes.find(e => String(e.episodeNumber) === String(episodeId))
                     || STATE.currentEpisodes[0];

        const epIndex = STATE.currentEpisodes.indexOf(episode);
        STATE.currentEpisodeIndex = epIndex;

        // Page title & heading
        document.title = `${movie.name} - ${episode.name || 'Tập ' + (epIndex + 1)} - SunPhim`;
        if (titleEl) titleEl.textContent = `${movie.name} - ${episode.name || 'Tập ' + (epIndex + 1)}`;
        document.getElementById('playerQualityBadge').textContent = movie.quality || '4K Ultra HD';

        // Ambient Ambilight Glow color styling
        const ambientGlow = document.getElementById('playerAmbientGlow');
        if (ambientGlow && movie.poster) {
            ambientGlow.style.backgroundImage = `radial-gradient(ellipse 70% 60% at 50% 50%, rgba(229, 9, 20, 0.28), rgba(0, 229, 255, 0.12), transparent 75%)`;
        }

        // Save progress to Continue Watching
        UTILS.saveContinueWatching(movie, episode, 25, 'Đang xem dở');

        // Render episodes grid
        renderWatchEpisodesGrid(movieSlug, epIndex);

        // Render movie info below player
        renderWatchMovieInfoCard(movie);

        // Prev & Next Episode Buttons
        setupEpisodeNavButtons(movieSlug, epIndex);

        // Cinema Controls: Theater, Lights Off, Fullscreen, Aspect Ratio, Server Selector
        setupCinemaControls(movieSlug, episode);

        // Load the actual video stream
        await mountStreamToPlayer(episode, STATE.currentServer);

        // Render Related Movies Row
        loadWatchRelatedMovies(movie);

    } catch (error) {
        console.error('Watch page init failed:', error);
        showPlayerError('Không thể nạp trình phát. Vui lòng thử lại sau.');
    }
}

// Stream Mounting with multi-server fallback
async function mountStreamToPlayer(episode, serverType = 'vip1') {
    const wrapper = document.getElementById('playerWrapper');
    if (!wrapper) return;

    if (serverType === 'vip1' && episode.embedLink) {
        // High speed VIP embed (Zero clipping)
        wrapper.innerHTML = `
            <iframe src="${episode.embedLink}"
                    title="SunPhim Cinema Player"
                    allowfullscreen
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share; fullscreen"
                    referrerpolicy="no-referrer"
                    loading="eager"></iframe>`;
    } else if (serverType === 'vip2' && episode.fileUrl && episode.fileUrl.includes('.m3u8')) {
        // VIP Server 2 - Native Hls.js Direct Stream
        wrapper.innerHTML = `<video id="cinemaVideo" controls playsinline autoplay style="width:100%; height:100%; background:#000;"><source src="${episode.fileUrl}" type="application/x-mpegURL"></video>`;
        const videoEl = document.getElementById('cinemaVideo');
        if (typeof Hls !== 'undefined' && Hls.isSupported()) {
            const hls = new Hls();
            hls.loadSource(episode.fileUrl);
            hls.attachMedia(videoEl);
        }
    } else if (episode.fileUrl) {
        // Direct MP4 fallback
        wrapper.innerHTML = `<video id="cinemaVideo" controls playsinline autoplay style="width:100%; height:100%; background:#000;"><source src="${episode.fileUrl}" type="video/mp4"></video>`;
    } else if (episode.embedLink) {
        wrapper.innerHTML = `
            <iframe src="${episode.embedLink}"
                    title="SunPhim Cinema Player"
                    allowfullscreen
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share; fullscreen"
                    referrerpolicy="no-referrer"></iframe>`;
    } else {
        showPlayerError('Nguồn phát cho tập phim này đang được kiểm duyệt.');
    }
}

function showPlayerError(message) {
    const wrapper = document.getElementById('playerWrapper');
    if (!wrapper) return;
    wrapper.innerHTML = `
        <div style="display:flex; flex-direction:column; align-items:center; justify-content:center; height:100%; color:#ef4444; gap:1rem; padding:2rem; text-align:center;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="48" height="48">
                <circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>
            </svg>
            <p style="font-weight:700; font-size:1.15rem; color:#fff;">${escapeHtml(message)}</p>
            <button class="btn btn-secondary" onclick="window.history.back()">Quay lại</button>
        </div>`;
}

// Cinema UI Toolbar Controls
function setupCinemaControls(movieSlug, episode) {
    const container = document.getElementById('playerContainer');
    const wrapper = document.getElementById('playerWrapper');

    // 1. Theater Mode Toggle (T)
    const theaterBtn = document.getElementById('theaterBtn');
    theaterBtn?.addEventListener('click', () => {
        STATE.theaterMode = !STATE.theaterMode;
        container?.classList.toggle('is-theater', STATE.theaterMode);
        theaterBtn.classList.toggle('active', STATE.theaterMode);
    });

    // 2. Lights Off Toggle (L)
    const lightsBtn = document.getElementById('lightsToggleBtn');
    const dimmer = document.getElementById('lightsDimmer');
    function toggleLights() {
        STATE.lightsOff = !STATE.lightsOff;
        document.body.classList.toggle('lights-off', STATE.lightsOff);
        lightsBtn?.classList.toggle('active', STATE.lightsOff);
    }
    lightsBtn?.addEventListener('click', toggleLights);
    dimmer?.addEventListener('click', toggleLights);

    // 3. Fullscreen Toggle (F)
    const fsBtn = document.getElementById('fullscreenBtn');
    fsBtn?.addEventListener('click', () => {
        if (!document.fullscreenElement) {
            wrapper?.requestFullscreen?.().catch(() => {});
        } else {
            document.exitFullscreen?.().catch(() => {});
        }
    });

    // 4. Aspect Ratio Toggle (16:9 vs 21:9)
    const ratioBtn = document.getElementById('ratioToggleBtn');
    const ratioLabel = document.getElementById('ratioLabel');
    ratioBtn?.addEventListener('click', () => {
        if (STATE.aspectRatio === '16-9') {
            STATE.aspectRatio = '21-9';
            wrapper?.classList.add('ratio-21-9');
            if (ratioLabel) ratioLabel.textContent = '21:9';
        } else {
            STATE.aspectRatio = '16-9';
            wrapper?.classList.remove('ratio-21-9');
            if (ratioLabel) ratioLabel.textContent = '16:9';
        }
    });

    // 5. Server Selector Dropdown
    const serverBtn = document.getElementById('serverSelectorBtn');
    const serverMenu = document.getElementById('serverMenu');
    serverBtn?.addEventListener('click', (e) => {
        e.stopPropagation();
        serverMenu?.classList.toggle('open');
    });

    document.addEventListener('click', () => serverMenu?.classList.remove('open'));

    serverMenu?.querySelectorAll('.server-item').forEach(item => {
        item.addEventListener('click', () => {
            const serverType = item.dataset.server;
            STATE.currentServer = serverType;
            serverMenu.querySelectorAll('.server-item').forEach(i => i.classList.remove('active'));
            item.classList.add('active');
            document.getElementById('currentServerLabel').textContent = item.querySelector('span').textContent;
            mountStreamToPlayer(episode, serverType);
            UTILS.showToast(`Đã chuyển sang ${item.querySelector('span').textContent}`, 'info');
        });
    });

    // 6. Keyboard Shortcuts Help Modal
    const helpBtn = document.getElementById('shortcutHelpBtn');
    const shortcutModal = document.getElementById('shortcutModalBackdrop');
    const shortcutClose = document.getElementById('shortcutModalClose');

    helpBtn?.addEventListener('click', () => shortcutModal?.classList.add('open'));
    shortcutClose?.addEventListener('click', () => shortcutModal?.classList.remove('open'));
    shortcutModal?.addEventListener('click', (e) => {
        if (e.target === shortcutModal) shortcutModal.classList.remove('open');
    });

    // 7. Global Keyboard Listeners
    document.addEventListener('keydown', (e) => {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
        if (e.key === 't' || e.key === 'T') theaterBtn?.click();
        if (e.key === 'l' || e.key === 'L') toggleLights();
        if (e.key === 'f' || e.key === 'F') fsBtn?.click();
        if (e.key === 'n' || e.key === 'N') document.getElementById('nextEpisode')?.click();
        if (e.key === 'p' || e.key === 'P') document.getElementById('prevEpisode')?.click();
        if (e.key === '?' || (e.shiftKey && e.key === '/')) helpBtn?.click();
        if (e.key === 'Escape') {
            if (STATE.lightsOff) toggleLights();
            shortcutModal?.classList.remove('open');
        }
    });
}

function setupEpisodeNavButtons(movieSlug, epIndex) {
    const prevBtn = document.getElementById('prevEpisode');
    const nextBtn = document.getElementById('nextEpisode');

    if (prevBtn) {
        prevBtn.disabled = epIndex <= 0;
        prevBtn.onclick = () => {
            if (epIndex > 0) {
                const targetEp = STATE.currentEpisodes[epIndex - 1];
                window.location.href = `/watch.html?movie=${movieSlug}&episode=${targetEp.id ?? epIndex}`;
            }
        };
    }

    if (nextBtn) {
        nextBtn.disabled = epIndex >= STATE.currentEpisodes.length - 1;
        nextBtn.onclick = () => {
            if (epIndex < STATE.currentEpisodes.length - 1) {
                const targetEp = STATE.currentEpisodes[epIndex + 1];
                window.location.href = `/watch.html?movie=${movieSlug}&episode=${targetEp.id ?? (epIndex + 2)}`;
            }
        };
    }
}

function renderWatchEpisodesGrid(movieSlug, activeIndex) {
    const grid = document.getElementById('episodesGrid');
    const countText = document.getElementById('episodesCountText');
    const searchInput = document.getElementById('episodeSearchInput');
    if (!grid) return;

    if (countText) countText.textContent = `Tổng cộng ${STATE.currentEpisodes.length} tập`;

    function renderList(list) {
        grid.innerHTML = list.map((ep, idx) => {
            const originalIdx = STATE.currentEpisodes.indexOf(ep);
            const isActive = originalIdx === activeIndex;
            return `
                <a href="/watch.html?movie=${movieSlug}&episode=${ep.id != null ? ep.id : originalIdx + 1}" class="episode-btn ${isActive ? 'active' : ''}">
                    ${escapeHtml(ep.name || `Tập ${ep.episodeNumber || originalIdx + 1}`)}
                </a>
            `;
        }).join('');
    }

    renderList(STATE.currentEpisodes);

    searchInput?.addEventListener('input', (e) => {
        const val = e.target.value.trim().toLowerCase();
        if (!val) {
            renderList(STATE.currentEpisodes);
            return;
        }
        const filtered = STATE.currentEpisodes.filter((ep, idx) => {
            const name = (ep.name || `Tập ${idx + 1}`).toLowerCase();
            return name.includes(val);
        });
        renderList(filtered);
    });
}

function renderWatchMovieInfoCard(movie) {
    const nameEl = document.getElementById('detailMovieName');
    const originEl = document.getElementById('detailMovieOrigin');
    const metaEl = document.getElementById('detailMovieMeta');
    const descEl = document.getElementById('detailMovieDesc');
    const bookmarkBtn = document.getElementById('watchBookmarkBtn');
    const shareBtn = document.getElementById('watchShareBtn');

    if (nameEl) nameEl.textContent = movie.name;
    if (originEl) originEl.textContent = movie.originName || '';
    if (descEl) descEl.textContent = movie.description || '';

    const score = movie.rating ?? movie.imdbScore ?? 8.5;
    if (metaEl) {
        metaEl.innerHTML = `
            <span class="rating-badge"><svg viewBox="0 0 24 24"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg> ${Number(score).toFixed(1)} IMDb</span>
            <span class="meta-pill quality">${movie.quality || '4K Ultra HD'}</span>
            <span class="meta-pill lang">${movie.lang || 'Vietsub'}</span>
            ${movie.year ? `<span class="meta-pill">${movie.year}</span>` : ''}
        `;
    }

    if (bookmarkBtn) {
        const inList = UTILS.isInWatchlist(movie.slug);
        bookmarkBtn.innerHTML = inList
            ? `<svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Đã lưu`
            : `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Lưu phim`;
        bookmarkBtn.onclick = () => {
            UTILS.toggleWatchlist(movie);
            const nowIn = UTILS.isInWatchlist(movie.slug);
            bookmarkBtn.innerHTML = nowIn
                ? `<svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Đã lưu`
                : `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> Lưu phim`;
        };
    }

    if (shareBtn) {
        shareBtn.onclick = () => {
            if (navigator.clipboard) {
                navigator.clipboard.writeText(window.location.href);
                UTILS.showToast('Đã sao chép liên kết xem phim vào bộ nhớ tạm!', 'success');
            } else {
                UTILS.showToast('Liên kết: ' + window.location.href, 'info');
            }
        };
    }
}

async function loadWatchRelatedMovies(movie) {
    const container = document.getElementById('relatedRow');
    if (!container) return;
    try {
        const trending = await API.home.getTrending(12);
        const filtered = (trending || []).filter(m => m.slug !== movie.slug).slice(0, 10);
        container.innerHTML = filtered.map(buildMovieCard).join('');
        initRowNavControls();
    } catch {
        document.getElementById('relatedSection')?.remove();
    }
}

// =============================================================================
// DETAIL PAGE LOADER (MOVIE.HTML)
// =============================================================================
async function loadMovieDetailPage(slug) {
    try {
        const movie = await API.movies.getBySlug(slug);
        if (!movie) {
            document.getElementById('detailTitle').textContent = 'Không tìm thấy phim';
            return;
        }

        STATE.currentMovie = movie;
        STATE.currentEpisodes = movie.episodes || [];

        // Meta & Document
        document.title = `${movie.name} - SunPhim`;
        document.getElementById('detailTitle').textContent = movie.name;
        document.getElementById('detailOriginName').textContent = movie.originName || '';
        document.getElementById('detailDescription').textContent = movie.description || '';

        // Backdrop & Poster
        const bgUrl = UTILS.moviePosterById(movie);
        document.getElementById('detailHero').style.backgroundImage = `url('${bgUrl}')`;
        const posterImg = document.getElementById('detailPoster');
        if (posterImg) {
            posterImg.src = UTILS.moviePosterById(movie);
            posterImg.alt = movie.name;
        }

        // Meta
        const score = movie.rating ?? movie.imdbScore ?? 8.5;
        document.getElementById('detailMeta').innerHTML = `
            <span class="rating-badge"><svg viewBox="0 0 24 24"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg> ${Number(score).toFixed(1)} IMDb</span>
            <span class="meta-pill quality">${movie.quality || '4K Ultra HD'}</span>
            <span class="meta-pill lang">${movie.lang || 'Vietsub'}</span>
            ${movie.year ? `<span class="meta-pill">${movie.year}</span>` : ''}
            ${movie.duration ? `<span class="meta-pill">${movie.duration} phút</span>` : ''}
        `;

        // Tags
        document.getElementById('detailTags').innerHTML = (movie.categories || ['Điện Ảnh']).map(c => `
            <span class="tag">${escapeHtml(c)}</span>
        `).join('');

        // Actions
        const firstEp = STATE.currentEpisodes[0];
        const playHref = firstEp ? `/watch.html?movie=${movie.slug}&episode=${firstEp.id || 1}` : '#';
        const inList = UTILS.isInWatchlist(movie.slug);

        document.getElementById('detailActions').innerHTML = `
            <a href="${playHref}" class="btn btn-primary" id="detailPlayBtn">
                <svg viewBox="0 0 24 24" fill="currentColor"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                Xem phim ngay
            </a>
            <button class="btn btn-secondary" id="detailWatchlistBtn">
                <svg viewBox="0 0 24 24" fill="${inList ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg>
                ${inList ? 'Đã lưu danh sách' : 'Lưu vào danh sách'}
            </button>
            <button class="btn btn-secondary" onclick="navigator.clipboard?.writeText(window.location.href); UTILS.showToast('Đã sao chép liên kết!', 'success');">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/><line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/></svg>
                Chia sẻ
            </button>
        `;

        document.getElementById('detailWatchlistBtn')?.addEventListener('click', () => {
            UTILS.toggleWatchlist(movie);
            const nowIn = UTILS.isInWatchlist(movie.slug);
            const btn = document.getElementById('detailWatchlistBtn');
            if (btn) {
                btn.innerHTML = `<svg viewBox="0 0 24 24" fill="${nowIn ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/></svg> ${nowIn ? 'Đã lưu danh sách' : 'Lưu vào danh sách'}`;
            }
        });

        // Episodes Matrix
        const totalLabel = document.getElementById('episodesTotalLabel');
        if (totalLabel) totalLabel.textContent = `Tổng cộng ${STATE.currentEpisodes.length} tập`;

        const epGrid = document.getElementById('episodesGrid');
        if (epGrid) {
            epGrid.innerHTML = STATE.currentEpisodes.map((ep, idx) => `
                <a href="/watch.html?movie=${movie.slug}&episode=${ep.id || idx + 1}" class="episode-btn">
                    ${escapeHtml(ep.name || `Tập ${idx + 1}`)}
                </a>
            `).join('');
        }

        // Similar Movies
        const simContainer = document.getElementById('similarRow');
        if (simContainer) {
            const trending = await API.home.getTrending(12);
            simContainer.innerHTML = (trending || []).filter(m => m.slug !== movie.slug).slice(0, 10).map(buildMovieCard).join('');
            initRowNavControls();
        }

    } catch (err) {
        console.error('Movie detail error:', err);
    }
}

// =============================================================================
// BROWSE & CATEGORY PAGES
// =============================================================================
async function loadBrowseByType(type, pageTitle) {
    showBrowseShell();
    document.getElementById('browseTitle').textContent = pageTitle;
    document.title = `${pageTitle} - SunPhim`;
    try {
        const movies = await API.movies.getAll(type);
        renderBrowseGrid(movies);
    } catch {
        renderBrowseGrid([]);
    }
}

async function loadBrowseByCategory(slug) {
    showBrowseShell();
    document.getElementById('browseTitle').textContent = slug.replace(/-/g, ' ').toUpperCase();
    document.title = `${slug.replace(/-/g, ' ')} - SunPhim`;
    try {
        const movies = await API.movies.getByCategory(slug);
        renderBrowseGrid(movies);
    } catch {
        renderBrowseGrid([]);
    }
}

async function loadWatchlistPage() {
    showBrowseShell();
    document.getElementById('browseTitle').textContent = 'Danh sách phim yêu thích của bạn';
    document.title = 'Danh sách của tôi - SunPhim';
    const list = UTILS.getWatchlist();
    renderBrowseGrid(list);
}

function renderBrowseGrid(movies) {
    const grid = document.getElementById('browseGrid');
    const empty = document.getElementById('browseEmpty');
    if (!grid) return;
    if (!movies || movies.length === 0) {
        grid.innerHTML = '';
        if (empty) empty.hidden = false;
        return;
    }
    if (empty) empty.hidden = true;
    grid.innerHTML = movies.map(buildMovieCard).join('');
}

function showBrowseShell() {
    document.getElementById('homeShell')?.setAttribute('hidden', 'true');
    document.getElementById('browseShell')?.removeAttribute('hidden');
}

function showHomeShell() {
    document.getElementById('homeShell')?.removeAttribute('hidden');
    document.getElementById('browseShell')?.setAttribute('hidden', 'true');
}

// =============================================================================
// ROUTER & BOOTSTRAP
// =============================================================================
function initHeaderScroll() {
    const header = document.getElementById('header');
    if (!header) return;
    window.addEventListener('scroll', () => {
        header.classList.toggle('scrolled', window.scrollY > 40);
    }, { passive: true });
}

function initMobileMenu() {
    const btn = document.getElementById('mobileMenuBtn');
    const nav = document.getElementById('mobileNav');
    if (!btn || !nav) return;
    btn.onclick = () => nav.classList.toggle('open');
}

function routeIndexApp() {
    const pathname = window.location.pathname.replace(/\/index\.html$/, '') || '/';

    // Update active nav links
    document.querySelectorAll('.nav-link, .mobile-nav-link').forEach(a => {
        a.classList.remove('active');
        if (a.getAttribute('href') === pathname) a.classList.add('active');
    });

    if (pathname === '/' || pathname === '') {
        showHomeShell();
        loadHomePage();
    } else if (pathname === '/phim-bo') {
        loadBrowseByType('series', 'Phim Bộ Tuyển Chọn');
    } else if (pathname === '/phim-le') {
        loadBrowseByType('single', 'Phim Lẻ Bom Tấn');
    } else if (pathname === '/danh-sach') {
        loadWatchlistPage();
    } else if (pathname.startsWith('/the-loai/')) {
        const slug = pathname.replace('/the-loai/', '').split('/')[0];
        loadBrowseByCategory(slug);
    } else {
        showHomeShell();
        loadHomePage();
    }
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function bootApp() {
    initHeaderScroll();
    initMobileMenu();
    initSpotlightSearch();

    // Global Esc listener for modals
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            closeQuickModal();
            document.getElementById('searchOverlay')?.classList.remove('active');
            document.body.style.overflow = '';
        }
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            document.getElementById('searchOverlay')?.classList.add('active');
            document.getElementById('searchOverlayInput')?.focus();
            document.body.style.overflow = 'hidden';
        }
    });

    if (document.getElementById('homeShell')) {
        routeIndexApp();
    }
}

// Window exports
if (typeof window !== 'undefined') {
    window.STATE = STATE;
    window.buildMovieCard = buildMovieCard;
    window.loadHomePage = loadHomePage;
    window.loadMovieDetailPage = loadMovieDetailPage;
    window.initWatchPageEngine = initWatchPageEngine;
    window.openQuickModal = openQuickModal;
    window.closeQuickModal = closeQuickModal;
    window.directPlayMovie = directPlayMovie;
    window.toggleWatchlistCard = toggleWatchlistCard;
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootApp);
} else {
    bootApp();
}
