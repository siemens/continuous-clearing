# Manual Update
   
## How to Download the source code manually for Debian components :
  In case if the CA Tool failed to upload the source code for any component following steps need to be done to update them manually.
       
-    Get Component name,Distro and version details of the components to upload.
-    Make a Clone request with the below given Url.
     
```
git clone https://gitlab.alpinelinux.org/alpine/aports

```
-   If Clone request completed,checkout specific Distro. 

```
git checkout Distroname

Ex:git checkout 3.16-stable

```
- If checkout request completed,through identify component folder by using component name.
  -In that folder contain APKBUILD,buildfiles and patch files found.
  -Open APKBUILD file,In this file identify source section,in that source section Source url,Build files and patch file names found.
    
 _`Sample APKBUILD file`_

 
```
# Contributor: Ariadne Conill <ariadne@dereferenced.org>
# Maintainer: Timo Teräs <timo.teras@iki.fi>
pkgname=musl
pkgver=1.2.3
pkgrel=3
pkgdesc="the musl c library (libc) implementation"
url="https://musl.libc.org/"
arch="all"
license="MIT"
options="lib64"
subpackages="
	$pkgname-dbg
	$pkgname-libintl:libintl:noarch
	$pkgname-dev
	libc6-compat:compat:noarch
	"
case "$BOOTSTRAP" in
nocc)	pkgname="musl-dev"; subpackages="";;
nolibc) ;;
*)	subpackages="$subpackages $pkgname-utils";;
esac
_commit="v1.2.3"
source="musl-$_commit.tar.gz::https://git.musl-libc.org/cgit/musl/snapshot/$_commit.tar.gz
	handle-aux-at_base.patch

	0001-fix-mishandling-of-errno-in-getaddrinfo-AI_ADDRCONFI.patch
	0001-fix-return-value-of-gethostby-name-2-addr-with-no-re.patch
	0001-fix-return-value-of-gethostnbyname-2-_r-on-result-no.patch
	0001-remove-impossible-error-case-from-gethostbyname2_r.patch

	relr-1.patch
	relr-2.patch
	relr-3.patch
	relr-4.patch

	ldconfig
	__stack_chk_fail_local.c
	getconf.c
	getent.c
	iconv.c
	"


```
Description for the settings in `APKBUILD` file

-In above sample file identify source section took source url,In that url identify "_commit" key word replace with given key value its found anyware in the apkbuild file.

```

_commit="v1.2.3"
https://git.musl-libc.org/cgit/musl/snapshot/$_commit.tar.gz

Ex:https://git.musl-libc.org/cgit/musl/snapshot/v1.2.3.tar.gz

```
 -Make a Get request of source Url,we downloaded source file. 
 
 -Extract that file andKeep all the downloaded files in one folder along with Patch file for applying the patch.

 -Goto extracted folder and Apply these commands for apply patch.
 
 ```
git init

git apply <path of patch file>/<patch file name>


```

-  After the successful execution of the command. 
-if build files found ,copy build files in one build files folder keep it on extracted folder.ZIP the `Folder` folder and rename it as `<source_Name>_<version><.tar.gz>`


-  Attach  the `<source_Name>_<version><.tar.gz>` file as SRC under the attachment section for the release in SW360.
