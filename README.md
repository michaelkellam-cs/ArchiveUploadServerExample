# ArchiveUploadServerExample
This project showcases an automated build script that will build, archive, and upload a Server build to a Linux server remotely.

[Medium Article](https://www.google.com)

Rundown of script:
1. Get directory of winrar.exe
2. Run batch script, taking in argument of winrar directory, and archiving specified Server files
3. Move archive to `BuildsForRemote`
4. Upload server file using SFTP
5. Delete file and `BuildsForRemote`
