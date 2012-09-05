#!/bin/bash

set -x

print_status_message() {
	echo ""
	echo "-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-"
	echo -en '\E[;32m'"MESSAGE: "
	echo "$1"
	tput sgr0
	echo "-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-"
	echo ""
}

./autogen.sh --prefix=`pwd`/build/bin --enable-debug && print_status_message "Configure: Complete" && \
make && print_status_message "Make: Complete" && \
make install && print_status_message "Make install: Complete" && \
make uninstall && print_status_message "Make uninstall: Complete" && \
make clean && print_status_message "Make clean: Complete" && \
./configure && print_status_message "Configure for distcheck: Complete" && \
make distcheck && print_status_message "Make distcheck: Complete" && \
exit 0 || \
echo -e '\E[;31m'"---------Build test failed---------" && \
tput sgr0 && \
exit 1
