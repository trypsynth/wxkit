#include "shim.h"

#include <wx/toolbar.h>

WXK_FUNCTION(wxk_Window*) wxk_ToolBarTool_GetToolBar(wxk_ToolBarTool* tool) noexcept {
	return wxk::Guard<wxk_Window*>(nullptr, [&]() -> wxk_Window* { return wxk::Wrap(static_cast<wxWindow*>(wxk::Unwrap(tool)->GetToolBar())); });
}
