cmake_minimum_required(VERSION 3.13)

set(PICO_SDK_PATH ../../pico-sdk CACHE FILEPATH "Path to the pico SDK (IC-Mapeamento)")
set(PICO_BOARD pico2 CACHE STRING "Board type (IC-Mapeamento")
set(PICO_PLATAFORM rp2350 CACHE STRING "Plataform type")

include(pico_sdk_import.cmake)

project(MapEletro C CXX ASM)

set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 17)
pico_sdk_init()

add_executable(MapEletro)

target_sources(MapEletro PUBLIC
		${CMAKE_CURRENT_LIST_DIR}/main.c
		${CMAKE_CURRENT_LIST_DIR}/usb_descriptors.c
)

# Make sure TinyUSB can find tusb_config.h
target_include_directories(MapEletro PUBLIC
		${CMAKE_CURRENT_LIST_DIR})

pico_enable_stdio_usb(MapEletro 1)
pico_enable_stdio_uart(MapEletro 1)
pico_add_extra_outputs(MapEletro)

target_link_libraries(MapEletro PUBLIC
		pico_stdlib
		hardware_adc
		hardware_irq
		hardware_dma
		pico_unique_id
		tinyusb_device
		tinyusb_board)
