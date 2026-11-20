cmake_minimum_required(VERSION 3.25)
project(wxkit LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# wxWidgets is always linked statically: into the application by Native AOT, or into wxkit's own
# shared library otherwise. Its options are forced here, rather than passed by the build script, so
# the generator can rely on exactly this set of features.
set(wxBUILD_SHARED OFF CACHE BOOL "" FORCE)
set(wxBUILD_MONOLITHIC OFF CACHE BOOL "" FORCE)
set(wxBUILD_SAMPLES OFF CACHE STRING "" FORCE)
set(wxBUILD_TESTS OFF CACHE STRING "" FORCE)
set(wxBUILD_DEMOS OFF CACHE BOOL "" FORCE)
set(wxBUILD_BENCHMARKS OFF CACHE BOOL "" FORCE)
set(wxBUILD_INSTALL OFF CACHE BOOL "" FORCE)
set(wxBUILD_COMPATIBILITY NONE CACHE STRING "" FORCE)
# Native AOT links the static C runtime, so everything it links has to agree with it.
set(wxBUILD_USE_STATIC_RUNTIME ON CACHE BOOL "" FORCE)
# wxAccessible is Microsoft Active Accessibility's, so only wxMSW has it.
if(WIN32)
	set(wxUSE_ACCESSIBILITY ON CACHE BOOL "" FORCE)
else()
	set(wxUSE_ACCESSIBILITY OFF CACHE BOOL "" FORCE)
endif()
# The Edge web view downloads its SDK from NuGet while configuring. It comes back with the web view itself.
set(wxUSE_WEBVIEW OFF CACHE BOOL "" FORCE)
set(wxUSE_WEBVIEW_EDGE OFF CACHE BOOL "" FORCE)
if(NOT DEFINED wxBUILD_DEBUG_LEVEL)
	set(wxBUILD_DEBUG_LEVEL 0 CACHE STRING "" FORCE)
endif()

if(UNIX)
	# Native AOT links position-independent executables, so every static library it links has to be
	# position independent too, as the shared library is.
	set(CMAKE_POSITION_INDEPENDENT_CODE ON)
	# A feature that needs a system library beyond GTK's would make every application need it, so
	# these are left out until WxKit binds what uses them: web requests need libcurl, the secret
	# store libsecret, sound SDL, and notifications libnotify.
	set(wxUSE_WEBREQUEST OFF CACHE BOOL "" FORCE)
	set(wxUSE_SECRETSTORE OFF CACHE BOOL "" FORCE)
	set(wxUSE_LIBSDL OFF CACHE BOOL "" FORCE)
	set(wxUSE_LIBNOTIFY OFF CACHE BOOL "" FORCE)
	# Spell checking needs gspell on Linux, and macOS has its own, so it is only left out on Linux.
	if(NOT APPLE)
		set(wxUSE_SPELLCHECK OFF CACHE BOOL "" FORCE)
		# Each function and piece of data gets a section of its own, so the linker's --gc-sections,
		# which Native AOT passes on Linux, can leave out what an application does not use, as
		# MSVC's /Gy and Apple's -dead_strip do elsewhere.
		add_compile_options(-ffunction-sections -fdata-sections)
	else()
		set(wxUSE_SPELLCHECK ON CACHE BOOL "" FORCE)
	endif()
	set(wxUSE_LIBMSPACK OFF CACHE BOOL "" FORCE)
	set(wxUSE_MEDIACTRL OFF CACHE BOOL "" FORCE)
	set(wxUSE_GLCANVAS OFF CACHE BOOL "" FORCE)
	# wxWidgets would use the system's image and compression libraries where it finds their headers.
	# Its own copies, as on Windows, keep an application from needing them, and the third-party
	# notices the same everywhere.
	foreach(library ZLIB LIBPNG LIBJPEG LIBTIFF LIBWEBP REGEX EXPAT)
		set(wxUSE_${library} builtin CACHE STRING "" FORCE)
	endforeach()
endif()

add_subdirectory(../wxWidgets wx EXCLUDE_FROM_ALL)

# The generated shim has a file for each class, so a statically linked application only loads the
# wxWidgets code of the classes it uses. They all include every header, so they share one
# precompiled header.
file(GLOB WXK_GENERATED CONFIGURE_DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/generated/*.cpp)

set(WXK_SOURCES
	${WXK_GENERATED}
	src/about.cpp
	src/accessible.cpp
	src/accessparts.cpp
	src/app.cpp
	src/clientdata.cpp
	src/clipboard.cpp
	src/dataview.cpp
	src/dropfiles.cpp
	src/event.cpp
	src/gridbag.cpp
	src/handlers/all.cpp
	src/notification.cpp
	src/numbers.cpp
	src/printing.cpp
	src/handlers/png.cpp
	src/handlers/jpeg.cpp
	src/handlers/tiff.cpp
	src/handlers/gif.cpp
	src/handlers/webp.cpp
	src/handlers/pnm.cpp
	src/handlers/pcx.cpp
	src/handlers/iff.cpp
	src/handlers/tga.cpp
	src/handlers/xpm.cpp
	src/handlers/icon.cpp
	src/menu.cpp
	src/object.cpp
	src/sizer.cpp
	src/string.cpp
	src/taskbar.cpp
	src/timer.cpp
	src/uiaction.cpp
	src/toolbar.cpp
	src/validator.cpp
	src/variant.cpp
	src/window.cpp
)

# The static library and the shared one are built from the same sources, and end up as
# wxkitnative.lib and wxkitnative.dll in directories of their own. The name is not wxkit because
# Windows file names ignore case, and wxkit.dll would collide with the managed WxKit.dll.
add_library(wxkitnative_static STATIC ${WXK_SOURCES})
target_include_directories(wxkitnative_static PUBLIC include)
target_link_libraries(wxkitnative_static PUBLIC wx::stc wx::core wx::base)
target_precompile_headers(wxkitnative_static PRIVATE generated/includes.h)
set_target_properties(wxkitnative_static PROPERTIES
	OUTPUT_NAME wxkitnative
	ARCHIVE_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/static>
)

add_library(wxkitnative SHARED ${WXK_SOURCES})
target_include_directories(wxkitnative PUBLIC include)
target_compile_definitions(wxkitnative PRIVATE WXK_BUILDING_SHARED)
target_link_libraries(wxkitnative PRIVATE wx::stc wx::core wx::base)
target_precompile_headers(wxkitnative PRIVATE generated/includes.h)
set_target_properties(wxkitnative PROPERTIES
	RUNTIME_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/shared>
	LIBRARY_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/shared>
	ARCHIVE_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/shared>
)

if(UNIX)
	# The shared library exports the shim's functions and nothing of wxWidgets, as on Windows, where
	# only what is marked for export is.
	set_target_properties(wxkitnative PROPERTIES CXX_VISIBILITY_PRESET hidden VISIBILITY_INLINES_HIDDEN ON)
	if(APPLE)
		target_link_options(wxkitnative PRIVATE "-Wl,-exported_symbol,_wxk_*" -Wl,-dead_strip)
	else()
		target_link_options(wxkitnative PRIVATE -Wl,--exclude-libs,ALL -Wl,--no-undefined -Wl,--gc-sections)
	endif()
endif()

if(WIN32)
	# wxMSW loads its cursors and bitmaps from the module it runs in. The shared library carries them
	# itself. A Native AOT application gets them as a .res file to link, which this object library makes.
	target_sources(wxkitnative PRIVATE src/wxkit.rc)
	add_library(wxkit_resources OBJECT src/wxkit.rc)
	target_link_libraries(wxkit_resources PRIVATE wx::core)
endif()
