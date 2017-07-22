
Push-Location
&git clone https://github.com/processhacker2/processhacker2.git "tmp"
&cd tmp;
&git checkout -b "Dependencies" dc6a8a94f7e4b381090b46eb8e1b9fd7de052dbe
&git apply "../ProcessHacker-Fix-__acrt_fp_format-bug-and-specify-CLR-compilation.patch"
&cp -r "phlib" "../"
&cp -r "phnt" "../"
&cp -r "tools/peview" "../"
&cd "../";
Remove-Item -Recurse -Force "tmp"
Pop-Location