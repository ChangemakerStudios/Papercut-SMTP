BuildVersion=$1
if test -z "$BuildVersion"; then
    echo "You must specify a build version. E.g.: 7.0.1"
    exit 1
fi

docker build -t changemakerstudiosus/papercut-smtp:"$BuildVersion" . --build-arg="BUILD_VERSION=7$BuildVersion" --no-cache