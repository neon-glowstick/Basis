# S3 Uploader
This package adds a button on the Avatar Sdk Gui in the in editor to upload your avatar to an S3 bucket.

### How to use this package

Add this line to dependencies in `Basis/Packages/manifest.json`
```
    "org.basisvr.contrib.upload.s3": "file:../../Contrib/Upload/S3",
```

### Uploading an avatar

1. Get a Cloudflare R2 bucket or an AWS S3 bucket, or any other S3 api compatible cloud storage.
2. Follow the instructions of your chosen provider to create a bucket. You probably want a public bucket that anyone can download from.
3. Follow your providers instructions to create an access token in order to obtain the Access Key and the Secret Key.
4. Put the keys, the service url, and your bucket name into the Avatar Sdk Gui inside Unity. Optionally you can save the config to easil retrieve it later.
5. If you havent already built your avatar, do so.
6. Click upload and wait. Both the assetbundle and the meta file will be uploaded.

The url to download the avatar will vary depending on your provider. Check their docs for how to get a public url to download the avatar from.
