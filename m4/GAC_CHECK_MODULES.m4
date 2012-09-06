# 
# GAC_CHECK_MODULES.m4
#  
# Author:
#       Antonius Riha <antoniusriha@gmail.com>
# 
# Copyright (c) 2012 Antonius Riha
# 
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
# 
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.
#
# PKG_PROG_GACUTIL
# ----------------------------------
AC_DEFUN([PKG_PROG_GACUTIL],
[AC_ARG_VAR([GACUTIL], [path to gacutil])
AC_ARG_VAR([GACUTIL_LIBDIR], [path overriding gacutil's built-in search path])

if test "x$ac_cv_env_GACUTIL_set" != "xset"; then
	AC_PATH_TOOL([GACUTIL], [gacutil])
fi[]dnl
])# PKG_PROG_GACUTIL

# GAC_CHECK_MODULES(VARIABLE-PREFIX, MODULES, [ACTION-IF-FOUND], [ACTION-IF-NOT-FOUND])
# --------------------------------------------------------------
AC_DEFUN([GAC_CHECK_MODULES],
[AC_REQUIRE([PKG_PROG_GACUTIL])dnl
AC_ARG_VAR([$1][_LIBS], [assembly reference compiler flag for $1, overriding gacutil])dnl

AC_MSG_CHECKING([for $1])

m4_define([_PKG_TEXT], [Alternatively, you may set the environment variables $1[]_LIBS
to avoid the need to call gacutil.])

if test -n "$GACUTIL_LIBDIR"; then
	gacdir_flag="/gacdir $GACUTIL_LIBDIR"
fi

if test -n "$$1[]_LIBS"; then
	AC_MSG_RESULT([yes, envvar: $$1[]_LIBS])
	$3
elif test -n $GACUTIL && n=$($GACUTIL -l $2 $gacdir_flag 2>/dev/null); then
	n=$(echo "$n" | tail -n1 | cut -d'=' -f2 | tr -d ' ')
	if test $n -gt 0; then
		$1[]_LIBS="-r:$2"
		AC_MSG_RESULT([yes gac: $$1[]_LIBS])
		$3
	else
   		AC_MSG_RESULT([no])
   		m4_default([$4], [AC_MSG_ERROR(
[Package requirements ($2) were not met.

Consider adjusting the GACUTIL_LIBDIR environment variable if you
installed software to a non-standard gac directory.

_PKG_TEXT])[]dnl
        ])
	fi
else
	AC_MSG_RESULT([no])
	m4_default([$4], [AC_MSG_FAILURE([gacutil could not be found.
Make sure it is in your PATH or set the GACUTIL environment
variable to the full path to gacutil.

_PKG_TEXT

])[]dnl
	])
fi[]dnl
])# GAC_CHECK_MODULES
