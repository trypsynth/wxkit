#include "shim.h"

#include <algorithm>

// Can't throw: a vector's size.
WXK_FUNCTION(size_t) wxk_Numbers_GetCount(const wxk_Numbers* numbers) noexcept {
	return numbers == nullptr ? 0 : numbers->values.size();
}

// Can't throw: copying numbers into memory .NET sized for them.
WXK_FUNCTION(void) wxk_Numbers_CopyTo(const wxk_Numbers* numbers, int64_t* values) noexcept {
	if (numbers != nullptr) std::copy(numbers->values.begin(), numbers->values.end(), values);
}

WXK_FUNCTION(void) wxk_Numbers_Free(wxk_Numbers* numbers) noexcept {
	delete numbers;
}
