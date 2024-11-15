using System.Net.Sockets;
using System.Text;

namespace Lab2.Server;

public class FileHandler : IDisposable
{
    private readonly TcpClient client;
    private FileStream fileStream;
    private string fileName;
    private long fileSize;
    private long readSize;
    private long lastReadSize;
    private TimeOnly startTime;
    private const int CHUNK_SIZE = 8192;
    private const int MAX_FILE_NAME_BYTE_SIZE = 4096;
    private const string OK_MESSAGE = "ok";
    private const int TIMER_DUE_TIME = 0;
    private const int TIMER_PERIOD = 3000;
    
    public FileHandler(TcpClient client)
    {
        this.client = client;
    }

    public async void HandleConnection()
    {
        using (this)
        {
            // Сервер первым шагом ожидает получить имя файла и размер от клиента, чтобы создать файл, куда будут писаться данные
            NetworkStream stream = this.client.GetStream();
            byte[] buffer = new byte[CHUNK_SIZE];
            await stream.ReadAsync(buffer, 0, CHUNK_SIZE);
            
            ParseFileMetadata(buffer);
            CreateFile();

            // Вторым шагом сервер отправляет подтверждающее сообщение "ок"
            await stream.WriteAsync(Encoding.ASCII.GetBytes(OK_MESSAGE), 0, OK_MESSAGE.Length);

            // Замер времени и начало работы счетчика скорости передачи данных
            this.startTime = TimeOnly.FromDateTime(DateTime.Now);
            Timer timer = new Timer((object? o) => { PrintSpeed(); }, null, TIMER_DUE_TIME, TIMER_PERIOD);


            while (this.readSize < this.fileSize)
            {
                int count = stream.ReadAsync(buffer, 0, CHUNK_SIZE).Result;
                await this.fileStream.WriteAsync(buffer, 0, count);
                this.readSize += count;
            }

            // Останавливаем работу таймера
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            // Последним шагом пишем клиенту сообщение подтверждения получения файла
            await stream.WriteAsync(Encoding.ASCII.GetBytes("done"), 0, "done".Length);

            PrintSpeed();
            Console.WriteLine("Файл получен удачно");
        }
    }

    private void ParseFileMetadata(byte[] buffer)
    {
        string stringifiedBuffer = Encoding.Default.GetString(buffer);
        string[] data = stringifiedBuffer.Split('/');
        this.fileName = data[0];
        this.fileSize = long.Parse(data[1]);
        Console.WriteLine(this.fileName);
    }

    private void CreateFile()
    {
        string fullFileName;
        if (!File.Exists("uploads/" + this.fileName))
        {
            fullFileName = "uploads/" + this.fileName;
        }
        else
        {
            fullFileName = "uploads/" + Random.Shared.Next()  + "_" + this.fileName;
        }
        this.fileStream = new FileStream(fullFileName, FileMode.CreateNew);
    }

    private void PrintSpeed()
    {
        TimeOnly now = TimeOnly.FromDateTime(DateTime.Now);
        double totalSpeed = this.readSize / (now - this.startTime).TotalSeconds / 1024 / 1024;
        double currentSpeed = (double)(this.readSize - this.lastReadSize) / double.Min((now - this.startTime).TotalSeconds, 3) / 1024 / 1024;
        this.lastReadSize = this.readSize;
        Console.WriteLine("общая скорость {0:F3} мнгновенная скорость {1:F3} МБ", totalSpeed, currentSpeed);
    }

    public void Dispose()
    {
        this.fileStream.Close();
        this.client.Close();
    }
}

