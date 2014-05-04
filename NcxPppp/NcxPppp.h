#pragma once
#include <windows.h>
#include <iostream>
#include <memory>
#include <conio.h>

using Platform::String;

namespace NcxPppp
{
    public ref class LibRrrr sealed
    {
    public:
		LibRrrr();
		String^ route(Platform::String^ path, int from, int to, double time, uint8 arriveBy);
	private:
		std::unique_ptr<char> LibRrrr::PlatformStringToCharArray(String^ string);
		std::wstring ToWString(const char* utf8String, unsigned int length);
		String^ ToPlatformString(const char* utf8String, unsigned int length);
    };
}