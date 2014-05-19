#pragma once
#include <windows.h>
#include <iostream>
#include <memory>
#include <conio.h>

using namespace Windows::Foundation;
using Platform::String;

namespace NcxPppp
{	

    public ref class LibRrrr sealed
    {
    public:
		LibRrrr();
		String^ route(Platform::String^ path, int from, int to, time_t time, uint8 arriveBy);
		IAsyncOperation<String^>^ routeAsync(Platform::String^ path, int from, int to, time_t time, uint8 arriveBy);
	private:
		std::unique_ptr<char> LibRrrr::PlatformStringToCharArray(String^ string);
		std::wstring ToWString(const char* utf8String, unsigned int length);
		String^ ToPlatformString(const char* utf8String, unsigned int length);
    };
}