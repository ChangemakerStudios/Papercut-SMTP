BuildVersion=$1
if [ -z "$BuildVersion" ]; then
    echo "You must specify a build version. E.g.: 7.0.1"
    exit 1
fi

BuildDate=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
VcsRef=$(git rev-parse --short HEAD)

docker build -t changemakerstudiosus/papercut-smtp:"$BuildVersion" . \
    --build-arg BUILD_VERSION="$BuildVersion" \
    --build-arg BUILD_DATE="$BuildDate" \
    --build-arg VCS_REF="$VcsRef" \
    --no-cache