#include <stdio.h>
#define _CRT_SECURE_NO_WARNINGS
#include <windows.h>
#include <setupapi.h>
#include <initguid.h>
#include <devguid.h>
#include <regstr.h>
#include <stdio.h>

#pragma comment(lib, "setupapi.lib")

void list_serial_ports() {
    HDEVINFO device_info_set;
    SP_DEVINFO_DATA device_info_data;
    DWORD i;

    // Get all devices of the Ports (COM & LPT) class
    device_info_set = SetupDiGetClassDevs(
        &GUID_DEVCLASS_PORTS,
        0,
        0,
        DIGCF_PRESENT
    );

    if (device_info_set == INVALID_HANDLE_VALUE) {
        printf("Error getting device list.\n");
        return;
    }

    device_info_data.cbSize = sizeof(SP_DEVINFO_DATA);

    for (i = 0; SetupDiEnumDeviceInfo(device_info_set, i, &device_info_data); i++) {
        char device_name[256];
        char port_name[256];
        DWORD size = 0;

        // Get the friendly name (e.g. "USB-SERIAL CH340 (COM3)")
        SetupDiGetDeviceRegistryPropertyA(
            device_info_set,
            &device_info_data,
            SPDRP_FRIENDLYNAME,
            NULL,
            (PBYTE)device_name,
            sizeof(device_name),
            &size
        );

        // Extract COM port name from friendly name
        char *start = strchr(device_name, '(');
        char *end = strchr(device_name, ')');

        if (start && end && (end > start)) {
            size_t len = end - start - 1;
            strncpy(port_name, start + 1, len);
            port_name[len] = '\0';
            printf("%s - %s\n", device_name, port_name);
        } else {
            printf("Found device: %s\n", device_name);
        }
    }

    SetupDiDestroyDeviceInfoList(device_info_set);
}

int main(void)
{
    printf("Hello, World!\n");
    list_serial_ports();
    return 0;
}
