// Variable
var {src, dest, ...gulp} = require('gulp'),
    browserSync = require('browser-sync').create(),
    concat = require('gulp-concat'),
    uglify = require('gulp-uglify'),
    minifyCSS = require('gulp-minify-css'),
    sourcemaps = require('gulp-sourcemaps'),
    sass = require('gulp-sass')(require('sass'));
    header = require('gulp-header');
 

// Gulp-SAAS
gulp.task('sass', function() {
  return src(['wwwroot/sass/style.scss'])
    .pipe(sourcemaps.init())
    .pipe(sass())
    .pipe(dest('wwwroot/css')) // concatinated css file
    .pipe(concat('style.min.css')) // concatinated css file wwwroot/sass/style.scss
    .pipe(minifyCSS({processImport: false}))
    .pipe(sourcemaps.write('.'))
    .pipe(dest('wwwroot/css')) // minified css file css/style.min.css
    .pipe(browserSync.reload({
      stream: true // watched by BrowserSync
    }))
});

gulp.task('responsive', function(){
  return src(['wwwroot/sass/responsive.scss'])
    .pipe(sourcemaps.init())
    .pipe(sass())
    .pipe(dest('wwwroot/css')) // concatinated css file
    .pipe(concat('responsive.min.css')) // concatinated css file wwwroot/sass/style.scss
    .pipe(minifyCSS({processImport: false}))
    .pipe(sourcemaps.write('.'))
    .pipe(dest('wwwroot/css')) // minified css file css/style.min.css
    .pipe(browserSync.reload({
      stream: true // watched by BrowserSync
    }))
});

gulp.task('icon', function(){
  return src('wwwroot/sass/icon/icon.scss')
    .pipe(sourcemaps.init())
    .pipe(sass())
    .pipe(dest('wwwroot/css')) // concatinated css file
    .pipe(concat('icon.min.css')) // concatinated css file wwwroot/sass/style.scss
    .pipe(minifyCSS({processImport: false}))
    .pipe(sourcemaps.write('.'))
    .pipe(dest('wwwroot/css')) // minified css file css/style.min.css
    .pipe(browserSync.reload({
      stream: true // watched by BrowserSync
    }))
});

gulp.task('vendors', function(){
  return src('wwwroot/sass/vendors/**/*.scss')
    .pipe(sourcemaps.init())
    .pipe(sass())
    .pipe(dest('wwwroot/css')) // concatinated css file
    .pipe(concat('vendors.min.css')) // concatinated css file wwwroot/sass/style.scss
    .pipe(minifyCSS({processImport: false}))
    .pipe(sourcemaps.write('.'))
    .pipe(dest('wwwroot/css')) // minified css file css/style.min.css
    .pipe(browserSync.reload({
      stream: true // watched by BrowserSync
    }))
});

// Gulp-concat & gulp-uglify 
gulp.task('concat-vendors', function () {
  src([
    'wwwroot/scripts/vendors/*.js'
  ])
   .pipe(concat('vendors.js')) // concatinated js file
   .pipe(dest('wwwroot/scripts')); // js/vendors.js

  src([
    'wwwroot/scripts/vendors/*.js'
  ])
   .pipe(concat('vendors.min.js'))
   .pipe(uglify({ output: { comments: /^!/ } })) // minified js file js/vendors.min.js 
   .pipe(dest('wwwroot/scripts')); // js/vendors.min.js
});

// Browser sync
gulp.task('browserSync', function(){
  browserSync.init({ 
    watchTask: true,
    online: true,
    server: {
      baseDir: './'
    }
  });
});

// Gulp watch
gulp.task('default', gulp.parallel('sass','icon','vendors','concat-vendors','responsive', function (done){
  gulp.watch('wwwroot/sass/**/*.scss').on('change', gulp.series('sass'));
  gulp.watch('wwwroot/sass/icon/*.scss').on('change', gulp.series('icon'));
  gulp.watch('wwwroot/sass/vendors/**/*.scss').on('change', gulp.series('vendors'));
  gulp.watch('wwwroot/scripts/vendors/*.js').on('change', gulp.series('concat-vendors'));
  gulp.watch(['wwwroot/sass/theme-responsive/**/*.scss', 'wwwroot/sass/responsive.scss']).on('change', gulp.series('responsive'));
  done()
}))