include Makefile.helpers
modname = DelayedDamage
dependencies = TinyHelper

assemble:
	# common for all mods
	rm -f -r public
	@make dllsinto TARGET=$(modname) --no-print-directory

forceinstall:
	echo "Cannot install helper dll as standalone mod"
