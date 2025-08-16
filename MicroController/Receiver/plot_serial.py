import serial
import matplotlib.pyplot as plt


def main():
    print("Welcome to plotter! Hope it works")
    ser = serial.Serial('COM11', baudrate=115200, timeout=1)
    lastTimestamp = 0
    ser.read(ser.inWaiting())

    plt.ion()
    fig, ax = plt.subplots()
    xdata, ydata, ydata2 = [], [], []
    line, = ax.plot(xdata, ydata)
    line2, = ax.plot(xdata, ydata2)

    while (1):
        data_available = ser.inWaiting()
        if data_available > 0:
            data = ser.read(data_available)
            #print(data)

            if data[0:4] != b'DATA':
                continue

            timestamp = int.from_bytes(data[4:12], 'little')
            firstChannel = data[12]
            numChannels = data[13]
            packetLen = int.from_bytes(data[14:16], 'little')
            
            print(f"Data received with t={timestamp}, firstChn={firstChannel}, numChn={numChannels}, packetLen={packetLen}")

           
            #print(data)
            chsData = []
        
            int16Data = [int.from_bytes(data[i:i+2], 'little') for i in range(16, 16 + packetLen, 2)]
                         
            for i in range(0, numChannels):
                chOffset = (i+numChannels-firstChannel) % numChannels
                chsData.append(int16Data[chOffset:chOffset + packetLen:numChannels])
        
            #print(chsData)
            print(timestamp - lastTimestamp)
            
            #print(len(int16Data))
            #print(chsData[0])
            #print(chsData[1])
            #print(len(chsData[2]))
            #data[16:16 + packetLen:numChannels]

            if lastTimestamp == 0: 
                lastTimestamp = timestamp
                continue
            
            xdata.extend([lastTimestamp + i*((timestamp - lastTimestamp)/len(chsData[0])) for i in range(0, len(chsData[0]))])
            ydata.extend(chsData[0])
            ydata2.extend(chsData[1])

            line.set_data(xdata, ydata)
            line2.set_data(xdata, ydata2)
            ax.relim()
            ax.set_xlim(lastTimestamp, timestamp)
            ax.autoscale_view(scalex=False, scaley=True)
            fig.canvas.draw()
            fig.canvas.flush_events()

            lastTimestamp = timestamp

if __name__ == "__main__":
    main()

