var { src, dest, parallel, series, watch, ...gulp } = require('gulp'),
    browserSync = require('browser-sync').create(),
    concat      = require('gulp-concat'),
    uglify      = require('gulp-uglify'),
    sass        = require('gulp-sass')(require('sass')),
    gulpif      = require('gulp-if'),
    postcss     = require('gulp-postcss'), // New
    cssnano     = require('cssnano'),      // New
    autoprefixer = require('autoprefixer'); // New

// --- CONFIGURATION ---
const paths = {
    cssOutput: 'wwwroot/css',
    jsOutput: 'wwwroot/scripts',
    // EXACT ORIGINAL ORDER - output.css last to ensure layers/resets don't break typography
    cssFiles: [
        'wwwroot/sass/vendors/**/*.scss',
        'wwwroot/sass/icon/icon.scss',
        'wwwroot/sass/style.scss',
        'wwwroot/sass/responsive.scss',
        'wwwroot/css/umbraco-blockgridlayout.css',
        'wwwroot/css/custom.css',
        'wwwroot/css/output.css' 
    ],
    jsFiles: [
        'wwwroot/scripts/jquery.js',       // 1. Load jQuery first
        'wwwroot/scripts/vendors/*.js',    // 2. Load all other vendor plugins
        'wwwroot/scripts/main.js'          // 3. Load your custom logic last
    ]
};

// --- STYLES TASK ---
gulp.task('styles', function() {
    // PostCSS plugins: Autoprefixer adds browser prefixes, cssnano minifies
    var processors = [
        autoprefixer(),
        cssnano({ preset: 'default' }) 
    ];

    return src(paths.cssFiles, { allowEmpty: true })
        .pipe(gulpif(f => f.path.endsWith('.scss'), sass.sync().on('error', sass.logError)))
        .pipe(concat('site.min.css'))
        .pipe(postcss(processors)) // Modern minification (No more split error!)
        .pipe(dest(paths.cssOutput))
        .pipe(browserSync.reload({ stream: true }));
});

// --- SCRIPTS TASK ---
gulp.task('scripts', function () {
    return src(paths.jsFiles, { allowEmpty: true })
        .pipe(concat('site.min.js')) // Renamed to site.min.js to reflect the bundle
        .pipe(uglify())
        .pipe(dest(paths.jsOutput))
        .pipe(browserSync.reload({ stream: true }));
});

// --- WATCH ---
gulp.task('default', series(parallel('styles', 'scripts'), function (done) {
    browserSync.init({
        proxy: "https://localhost:44321", 
        notify: false, ui: false, open: false
    });

    watch('wwwroot/sass/**/*.scss', series('styles'));
    watch('wwwroot/css/custom.css', series('styles'));
    watch('wwwroot/css/output.css', series('styles'));
    watch('wwwroot/scripts/vendors/*.js', series('scripts'));
    watch('Views/**/*.cshtml').on('change', browserSync.reload);
    done();
}));