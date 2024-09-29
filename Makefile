include Makefile.helpers
modname = DelayedDamage
dependencies = TinyHelper

assemble:
	# common for all mods
	rm -f -r public
	@make dllsinto TARGET=$(modname) --no-print-directory

publish:
	make assemble
	rm -f $(modname).rar
	rar a $(modname).rar -ep1 public/*

install:
	echo "Cannot install helper dll as standalone mod"
