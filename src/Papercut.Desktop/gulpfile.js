const electronOpts = {
    version: '1.7.8',
    productAppName: 'Papercut',
    productDisplayName: 'Papercut Simple Desktop SMTP Server',
    companyName: 'ChangemakerStudios',
    copyright: `Copyright (C) ${new Date().getFullYear()} ChangemakerStudios`,
    darwinIcon: 'icons/Papercut-icon.icns',
    darwinBundleIdentifier: 'com.papercut.netcore',
    platform: 'darwin',
    arch: 'x64',
    ffmpegChromium: true,
    keepDefaultApp: true
};

const gulp = require('gulp');
const symdest = require('gulp-symdest');
const electron = require('gulp-atom-electron');
const es = require('event-stream');
const json = require('gulp-json-editor');

const destDir = 'bin/desktop/Papercut-darwin-x64';

function packageApp(fromHost) {
    let dir = fromHost ? 'obj/Host' : 'obj/desktop/darwin';
    return function() {
        let version = '5.5.0';
        if (!!process.env.PAPERCUT_RELEASE_VERSION){
            version = process.env.PAPERCUT_RELEASE_VERSION;
        }
        
        let packageJson = gulp.src(['package.json'], { base: '.' }).pipe(json({ version }));
        let sources = gulp.src([`${dir}/**/*.*`, `!${dir}/node_modules/electron/**/*.*`]);

        let extraOptions = {};
        if (!!process.env.GITHUB_AUTH_TOKEN){
            extraOptions.token = process.env.GITHUB_AUTH_TOKEN;
        }
        
        return es.merge(packageJson, sources)
            .pipe(electron(Object.assign({}, extraOptions, electronOpts)))
            .pipe(symdest(destDir));
    };
}

gulp.task('clean', function (cb) {
    const path = require('path');
    const rimraf = require('rimraf');

    rimraf(path.join(__dirname, destDir), cb);
});

gulp.task('default', ['clean'], packageApp(true));

gulp.task('release', ['clean'], packageApp(false));