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
# The web view is the system's browser engine: Edge's WebView2 on Windows, whose SDK wxWidgets
# downloads from NuGet while configuring, and whose loader is linked in, so nothing ships beside
# the application; WebKit on macOS; and WebKitGTK on Linux. Internet Explorer's is left out.
set(wxUSE_WEBVIEW ON CACHE BOOL "" FORCE)
if(WIN32)
	set(wxUSE_WEBVIEW_EDGE ON CACHE BOOL "" FORCE)
	set(wxUSE_WEBVIEW_EDGE_STATIC ON CACHE BOOL "" FORCE)
	set(wxUSE_WEBVIEW_IE OFF CACHE BOOL "" FORCE)
endif()
# wxWidgets checks its preconditions with assertions, which it recommends keeping in release builds,
# and a failed one raises Application.AssertFailed, so applications learn of misuse that would
# otherwise go unnoticed. WXKIT_DEBUG_LEVEL 0 compiles them out.
option(WXKIT_SANITIZE "Build wxWidgets and the shim with AddressSanitizer and UndefinedBehaviorSanitizer" OFF)
# For scripts/test-sanitizers.ps1: wxWidgets and the shim report memory errors and undefined behavior
# as the tests run. What they find in wxWidgets, WxKit fixes with a patch in native/patches until
# wxWidgets has it. Line numbers are enough for the reports, and keep the library a reasonable size.
if(WXKIT_SANITIZE)
	add_compile_options(-fsanitize=address,undefined -fno-omit-frame-pointer -g1)
	add_link_options(-fsanitize=address,undefined)
endif()
set(WXKIT_DEBUG_LEVEL 1 CACHE STRING "wxWidgets' debug level: 1 keeps its assertions, 0 compiles them out")
set(wxBUILD_DEBUG_LEVEL ${WXKIT_DEBUG_LEVEL} CACHE STRING "" FORCE)

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
# The web view's, which the shared library on Linux leaves out, since WebKitGTK would then be
# needed by every application, and which a library of its own has instead.
file(GLOB WXK_WEBVIEW CONFIGURE_DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/generated/WebView*.cpp)
list(APPEND WXK_WEBVIEW ${CMAKE_CURRENT_SOURCE_DIR}/src/webview.cpp)
list(REMOVE_ITEM WXK_GENERATED ${WXK_WEBVIEW})

set(WXK_SOURCES
	${WXK_GENERATED}
	src/about.cpp
	src/accessinfo.cpp
	src/accessible.cpp
	src/accessparts.cpp
	src/app.cpp
	src/appearance.cpp
	src/clientdata.cpp
	src/clipboard.cpp
	src/credentials.cpp
	src/dataview.cpp
	src/dragdrop.cpp
	src/dropfiles.cpp
	src/event.cpp
	src/labels.cpp
	src/handlers/all.cpp
	src/numbers.cpp
	src/printing.cpp
	src/imagedata.cpp
	src/liveregion.cpp
	src/translations.cpp
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
	src/object.cpp
	src/string.cpp
	src/taskbar.cpp
	src/toolbar.cpp
	src/validator.cpp
	src/variant.cpp
	src/window.cpp
)

# The static library and the shared one are built from the same sources, and end up as
# wxkitnative.lib and wxkitnative.dll in directories of their own. The name is not wxkit because
# Windows file names ignore case, and wxkit.dll would collide with the managed WxKit.dll.
add_library(wxkitnative_static STATIC ${WXK_SOURCES} ${WXK_WEBVIEW})
target_include_directories(wxkitnative_static PUBLIC include)
target_link_libraries(wxkitnative_static PUBLIC wx::webview wx::stc wx::core wx::base)
target_precompile_headers(wxkitnative_static PRIVATE generated/includes.h)
set_target_properties(wxkitnative_static PROPERTIES
	OUTPUT_NAME wxkitnative
	ARCHIVE_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/static>
)

# A shared library, built from the shim's sources, with the web view's or without. On Linux, the
# one with it goes in a directory of its own, shared/webview, for applications that set WxKitWebView.
function(wxk_add_shared target directory)
	add_library(${target} SHARED ${ARGN})
	target_include_directories(${target} PUBLIC include)
	target_compile_definitions(${target} PRIVATE WXK_BUILDING_SHARED)
	target_link_libraries(${target} PRIVATE wx::stc wx::core wx::base)
	target_precompile_headers(${target} PRIVATE generated/includes.h)
	set_target_properties(${target} PROPERTIES
		OUTPUT_NAME wxkitnative
		RUNTIME_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/${directory}>
		LIBRARY_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/${directory}>
		ARCHIVE_OUTPUT_DIRECTORY $<1:${CMAKE_BINARY_DIR}/${directory}>
	)
	if(UNIX)
		# The shared library exports the shim's functions and nothing of wxWidgets, as on Windows, where
		# only what is marked for export is.
		set_target_properties(${target} PROPERTIES CXX_VISIBILITY_PRESET hidden VISIBILITY_INLINES_HIDDEN ON)
		if(APPLE)
			target_link_options(${target} PRIVATE "-Wl,-exported_symbol,_wxk_*" -Wl,-dead_strip)
		else()
			target_link_options(${target} PRIVATE -Wl,--exclude-libs,ALL -Wl,--no-undefined -Wl,--gc-sections)
		endif()
	endif()
endfunction()

if(UNIX AND NOT APPLE)
	wxk_add_shared(wxkitnative shared ${WXK_SOURCES})
	wxk_add_shared(wxkitnative_webview shared/webview ${WXK_SOURCES} ${WXK_WEBVIEW})
	target_link_libraries(wxkitnative_webview PRIVATE wx::webview)
else()
	wxk_add_shared(wxkitnative shared ${WXK_SOURCES} ${WXK_WEBVIEW})
	target_link_libraries(wxkitnative PRIVATE wx::webview)
endif()

if(WIN32)
	# wxMSW loads its cursors and bitmaps from the module it runs in. The shared library carries them
	# itself. A Native AOT application gets them as a .res file to link, which this object library makes.
	target_sources(wxkitnative PRIVATE src/wxkit.rc)
	add_library(wxkit_resources OBJECT src/wxkit.rc)
	target_link_libraries(wxkit_resources PRIVATE wx::core)
endif()
