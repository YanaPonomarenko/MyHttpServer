using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyHttpServer;

class User
{
    public string Login { get; set; } = null!;
    public string Pwd { get; set; } = null!;
    public override string ToString()
    {
        return $"Login: {Login} Password {Pwd}";
    }
}


public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}

internal class Server
{
    readonly string _HOST = "http://127.0.0.1:8081/";

    private List<Student> _students; 

    public Server()
    {

        _students = new List<Student>
        {
            new Student { Id = 1, Name = "Іван", Surname = "Петренко", Group = "ІП-101" },
            new Student { Id = 2, Name = "Марія", Surname = "Шевченко", Group = "ІП-102" },
            new Student { Id = 3, Name = "Петро", Surname = "Коваленко", Group = "ІП-101" },
            new Student { Id = 4, Name = "Олена", Surname = "Бондаренко", Group = "КН-201" },
            new Student { Id = 5, Name = "Андрій", Surname = "Мельник", Group = "ІП-102" },
            new Student { Id = 6, Name = "Наталія", Surname = "Лисенко", Group = "КН-201" },
            new Student { Id = 7, Name = "Володимир", Surname = "Ткаченко", Group = "ІП-101" },
            new Student { Id = 8, Name = "Катерина", Surname = "Савченко", Group = "ІП-102" },
            new Student { Id = 9, Name = "Олександр", Surname = "Кравченко", Group = "КН-202" },
            new Student { Id = 10, Name = "Тетяна", Surname = "Олійник", Group = "КН-202" }
        };
    }

    public async Task RunServer()
    {
        HttpListener server = new HttpListener();
        server.Prefixes.Add(_HOST);
        server.Start();
        Console.WriteLine($"Server has been started {_HOST}");
        while (true)
        {
            try
            {

                HttpListenerContext ctx = await server.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                if (req.HttpMethod == "GET")
                {
                    Console.WriteLine($"Request: {req.Url} {req.HttpMethod} {req.Url?.AbsolutePath}");
                    string? param = req.Url?.AbsolutePath ?? "/";

                    if (param != null && param.StartsWith("/student/") && param.Length > "/student/".Length)
                    {
                        string idString = param.Substring("/student/".Length);
                        if (int.TryParse(idString, out int id))
                        {
                            await GetStudentById(ctx.Response, id);
                            continue;
                        }
                    }

                    if (param == "/student")
                    {
                        await GetAllStudents(ctx.Response);
                        continue;
                    }
                    if (param != null)
                    {
                        param = "/";
                    if(param != null)
                        {
                            var queryString = req.QueryString;
                            if (queryString != null)
                            {
                                Console.WriteLine($"QUERY PARAMS: {queryString["login"]} {queryString["pwd"]}");
                            }
                        }
                       
                    }
                    string page = GetPageName(param);
                    HttpListenerResponse res = ctx.Response;
                    string path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "pages", "index.html");
                    string html = await File.ReadAllTextAsync(path, req.ContentEncoding);
                    byte[] bytes = Encoding.UTF8.GetBytes(html);
                    res.ContentType = "text/html; charset=utf-8";
                    res.StatusCode = 200;

                    using (Stream stream = res.OutputStream)
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    res.Close();
                }
                else if (req.HttpMethod == "POST")
                {
                    string body = "";
                    using(var reader = new StreamReader(req.InputStream,req.ContentEncoding))
                    {
                        body = await reader.ReadToEndAsync();
                        try
                        {
                            var user = JsonSerializer.Deserialize<User>(body);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private async Task GetAllStudents(HttpListenerResponse response)
    {
        string html = "<html><body><h1>Список студентів</h1><ul>";

        foreach (var student in _students)
        {
            html += $"<li>ID: {student.Id} - {student.Surname} {student.Name} - Група: {student.Group}</li>";
        }

        html += "</ul><a href='/'>На головну</a></body></html>";
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.StatusCode = 200;

        using (Stream stream = response.OutputStream)
        {
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
        response.Close();
    }

    private async Task GetStudentById(HttpListenerResponse response, int id)
    {
        Student? student = _students.FirstOrDefault(s => s.Id == id);

        string html = "<html><body>";

        if (student != null)
        {
            html += "<h1>Інформація про студента</h1>";
            html += $"<p>ID: {student.Id}</p>";
            html += $"<p>Ім'я: {student.Name}</p>";
            html += $"<p>Прізвище: {student.Surname}</p>";
            html += $"<p>Група: {student.Group}</p>";
        }
        else
        {
            html += "<h1>Студента не знайдено</h1>";
            html += $"<p>Студент з ID {id} не існує</p>";
        }

        html += "<br><a href='/student'>Назад до списку</a>";
        html += "<br><a href='/'>На головну</a>";
        html += "</body></html>";

        byte[] bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.StatusCode = 200;

        using (Stream stream = response.OutputStream)
        {
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
        response.Close();
    }

    private string GetPageName(string param)
    {
        string result = param switch
        {
            "/contacts" => "contacts.html",
            "/about" => "about.html",
            "/" => "index.html",
            _ => "notfound.html"
        };
        return result;
    }
}
