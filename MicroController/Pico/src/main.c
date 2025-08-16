#include <stdlib.h>
#include <pico/stdlib.h>
#include <bsp/board_api.h>
#include <tusb.h>
#include <pico/stdio.h>
#include "hardware/gpio.h"
#include "hardware/adc.h"
#include "hardware/dma.h"
#include "hardware/irq.h"

#define ADC_FREQ 48000000
#define ADC_CHANNELS 2
#define ADC_CHANNEL_MASK ((1<<ADC_CHANNELS) - 1)
// This needs to be of form ADC_FREQ/(ADC_CHANNELS*n), where n is a integer.
// Max ADC sample rate is 500_000 samples/s. Channels are multiplexed
//#define ADC_CHANNEL_SAMPLE_RATE 10000
#define ADC_CHANNEL_SAMPLE_RATE 10000
#define ADC_CLKDIV ((ADC_FREQ / (ADC_CHANNELS*ADC_CHANNEL_SAMPLE_RATE)) - 1)

#define PICO_DEFAULT_LED_PIN 25
//(500k/s)
// 500000	- 1s
// 5000		- 10ms
// 1000		- 2ms
#define NUM_BUFFERS 2
#define NUM_SAMPLES_PER_BUFFER 1000
#define NUM_BYTES_PER_BUFFER (NUM_SAMPLES_PER_BUFFER*sizeof(uint16_t))
uint16_t adc_buffer1[NUM_SAMPLES_PER_BUFFER];
uint16_t adc_buffer2[NUM_SAMPLES_PER_BUFFER];

static int dma_chan = -1;
static uint16_t adcBuffers[NUM_BUFFERS][NUM_SAMPLES_PER_BUFFER] = {{},{}};
static uint8_t dmaBufferIdx = 0;
static uint8_t lastDmaBufferIdx= -1;

static uint16_t sampleBuff[NUM_SAMPLES_PER_BUFFER];

void custom_cdc_task(void);
void dma_handler();

int main() {
	// Initialize TinyUSB stack
	board_init();
	tusb_init();

	// TinyUSB board init callback after init
	if (board_init_after_tusb) {
		board_init_after_tusb();
	}
	//tu_fifo_set_overwritable()

	stdio_init_all();
	printf("USB Initialized\n");

	for (int i = 0; i < NUM_SAMPLES_PER_BUFFER; i++) {
		//sampleBuff[i] = __builtin_bswap16((uint16_t)((((int)'A') << 8) + (int)'a'));
		sampleBuff[i] = __builtin_bswap16((uint16_t)((((int)('A' + (i % ADC_CHANNELS))) << 8) + (int)('a' + (i % ADC_CHANNELS))));
	}

	sampleBuff[0] = __builtin_bswap16(((uint16_t)((((int)'1') << 8) + (int)'2')));
	sampleBuff[NUM_SAMPLES_PER_BUFFER-1] = __builtin_bswap16(((uint16_t)((((int)'P') << 8) + (int)'U')));

 	//sleep_ms(5000);
	//printf("aaaaaaaaa");
	adc_init();

	// Make sure GPIO is high-impedance, no pullups etc
	adc_gpio_init(26);
	adc_gpio_init(27);
	adc_gpio_init(28);
	adc_gpio_init(29);

	adc_select_input(0);
	/*
	*This function sets which inputs are to be run through in round-robin mode. RP2040, RP2350 QFN-60: Value between 0
and 0x1f (bit 0 to bit 4 for GPIO 26 to 29 and temperature sensor input respectively) RP2350 QFN-80: Value between 0
and 0xff (bit 0 to bit 7 for GPIO 40 to 47 and temperature sensor input respectively)

		bitmask 0x1f -> 0001_1111
		temp sensor . 29 . 28 . 27 . 26
*/
	adc_set_round_robin(ADC_CHANNEL_MASK);
	adc_fifo_setup(
		true,    // Write each completed conversion to the sample FIFO
		true,    // Enable DMA data request (DREQ)
		1,       // DREQ (and IRQ) asserted when at least 1 sample present
		false,   // Error bit
		false     // Shift each sample to 8 bits when pushing to FIFO
	);

	const int frac_bit_count = REG_FIELD_WIDTH(ADC_DIV_FRAC);
	adc_hw->div = (uint32_t)(ADC_CLKDIV * (1 << frac_bit_count));

	// Set up the DMA to start transferring data as soon as it appears in FIFO
	dma_chan = dma_claim_unused_channel(true);
	dma_channel_config cfg = dma_channel_get_default_config(dma_chan);

	// Reading from constant address, writing to incrementing byte addresses
	channel_config_set_transfer_data_size(&cfg, DMA_SIZE_16);
	channel_config_set_read_increment(&cfg, false);
	channel_config_set_write_increment(&cfg, true);
	channel_config_set_irq_quiet(&cfg, false);

	// Pace transfers based on availability of ADC samples
	channel_config_set_dreq(&cfg, DREQ_ADC);

	dmaBufferIdx = 0;
	dma_channel_configure(dma_chan, &cfg,
		adcBuffers[dmaBufferIdx],    // dst
		&adc_hw->fifo,  // src
		NUM_SAMPLES_PER_BUFFER,  // transfer count
		false            // start immediately
	);

	dma_channel_set_irq0_enabled(dma_chan, true);

	// Configure the processor to run dma_handler() when DMA IRQ 0 is asserted
	irq_set_exclusive_handler(DMA_IRQ_0, dma_handler);
	irq_set_enabled(DMA_IRQ_0, true);

	dma_channel_start(dma_chan);

	adc_run(true);

	gpio_init(PICO_DEFAULT_LED_PIN);
	gpio_set_dir(PICO_DEFAULT_LED_PIN, GPIO_OUT);
	gpio_put(PICO_DEFAULT_LED_PIN, true);


	while(true) {
		// TinyUSB device task | must be called regurlarly
		tud_task();

		// custom tasks
		//custom_cdc_task();
	}
}

void custom_cdc_task() {
	static uint32_t nextTrigger = 0;
	if (board_millis() < nextTrigger) return;
	nextTrigger = board_millis() + 2000;

	if (tud_cdc_n_connected(1)) {
		// print on CDC 0 some debug message
		printf("Connected to CDC 0\n");
		tud_cdc_n_write(1, "START MESSAGE ----------", 24);
		tud_cdc_n_write(1, sampleBuff, NUM_SAMPLES_PER_BUFFER * 2);
		tud_cdc_n_write(1, " ----------- END MESSAGE", 24);
		for (int i = 0; i < NUM_SAMPLES_PER_BUFFER; i+= 1024) {
			printf("i=%d\n", i);
			//tud_cdc_n_write(1, (sampleBuff) + i, NUM_SAMPLES * 2);

		}
		tud_cdc_n_write_flush(1);
	}
}

void tud_cdc_rx_cb(uint8_t itf)
{
	// allocate buffer for the data in the stack
	uint8_t buf[CFG_TUD_CDC_RX_BUFSIZE];

	printf("RX CDC %d\n", itf);

	// read the available data
	// | IMPORTANT: also do this for CDC0 because otherwise
	// | you won't be able to print anymore to CDC0
	// | next time this function is called
	uint32_t count = tud_cdc_n_read(itf, buf, sizeof(buf));
	printf("Count %d\n", count);

	// check if the data was received on the second cdc interface
	if (itf == 1) {
		// process the received data
		buf[count] = 0; // null-terminate the string
		// now echo data back to the console on CDC 0
		printf("Received on CDC 1: %s\n", buf);

		// and echo back OK on CDC 1
		tud_cdc_n_write(itf, (uint8_t const *) "OK\r\n", 4);
		tud_cdc_n_write_flush(itf);
	}
}


void __not_in_flash_func(dma_handler()) {
	static uint64_t lastInvokedTime = 0;
	if (!dma_channel_get_irq0_status(dma_chan)) return;

	lastDmaBufferIdx = dmaBufferIdx;
	if (++dmaBufferIdx >= NUM_BUFFERS) dmaBufferIdx = 0;
	dma_hw->ints0 = 1u << dma_chan;

	dma_channel_set_write_addr(dma_chan, adcBuffers[dmaBufferIdx], true);
	printf("ADC FIFO has %d elements  | ", adc_fifo_get_level());
	const uint64_t tmp = lastInvokedTime;
	lastInvokedTime = to_us_since_boot(get_absolute_time());
	printf("DMA IRQ Triggered, deltaT = %llu ms (%llu us), expected to be %f mas | " , (lastInvokedTime- tmp)/1000, lastInvokedTime - tmp, NUM_SAMPLES_PER_BUFFER/(ADC_CHANNELS * (ADC_CHANNEL_SAMPLE_RATE/1000.0f)));


	// Clear the interrupt request.




	long sum = 0;
	for (int i = 0; i < NUM_SAMPLES_PER_BUFFER; i++) {
		sum += adcBuffers[lastDmaBufferIdx][i];
		//adcBuffers[lastDmaBufferIdx][i] |= 0b1111 << 12;
//		adcBuffers[lastDmaBufferIdx][i] = __builtin_bswap16(adcBuffers[lastDmaBufferIdx][i]);
	}
	printf("ADC FIFO has %d elements  | ", adc_fifo_get_level());
	//

	uint64_t sample_time = lastInvokedTime- tmp;
	uint8_t header[] = {
		// Preamble - 4 bytes
		'D', 'A', 'T', 'A',
		// Timestamp - 8 bytes (12 bytes total) - Little Endian
		(uint8_t) lastInvokedTime & 0xff,
		(uint8_t) ((lastInvokedTime >> 8) & 0xff),
		(uint8_t) ((lastInvokedTime >> 16) & 0xff),
		(uint8_t) ((lastInvokedTime >> 24) & 0xff),
		(uint8_t) ((lastInvokedTime >> 32) & 0xff),
		(uint8_t) ((lastInvokedTime >> 40) & 0xff),
		(uint8_t) ((lastInvokedTime >> 48) & 0xff),
		(uint8_t) ((lastInvokedTime >> 56) & 0xff),
		// First two bytes refer to channel ? - 1 byte (13 bytes total)
		(uint8_t) ((-((int)adc_get_selected_input() - NUM_SAMPLES_PER_BUFFER)) % ADC_CHANNELS),
		// Number of channels - 1 byte (14 bytes)
		ADC_CHANNELS,
		// Packet Length - 2 bytes (16 bytes total)
		(uint8_t) ((NUM_BYTES_PER_BUFFER) & 0xff),
		(uint8_t) ((NUM_BYTES_PER_BUFFER >> 8) & 0xff),
		// Sample Time - 4 bytes (20 bytes total)
		(uint8_t) ((sample_time) & 0xff),
		(uint8_t) ((sample_time >> 8) & 0xff),
		(uint8_t) ((sample_time >> 16) & 0xff),
		(uint8_t) ((sample_time >> 24) & 0xff)
	};

	tud_cdc_n_write(1, header, sizeof(header));
	//tud_cdc_n_write_flush(1);
	//tud_cdc_n_write(1, "START MESSAGE ----------", 24);
	tud_cdc_n_write(1, adcBuffers[lastDmaBufferIdx], NUM_BYTES_PER_BUFFER);
	//tud_cdc_n_write(1, " ------ END MESSAGE\n\r", 21);
	tud_cdc_n_write_flush(1);

	//tud_cdc_n_write(0, prev_buffer, NUM_SAMPLES);
	printf("Avg: %d\n", sum/NUM_SAMPLES_PER_BUFFER);
}
