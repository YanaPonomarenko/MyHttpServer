using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Resources;
using System.Text;
using System.Web;
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
    private List<User> _users;

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
        _users = new List<User>();
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
                HttpListenerResponse response = ctx.Response;

                if (req.HttpMethod == "GET")
                {
                    Console.WriteLine($"Request: {req.Url} {req.HttpMethod} {req.Url?.AbsolutePath}");
                    string? param = req.Url?.AbsolutePath ?? "/";

                    if (param != null && param.StartsWith("/student/") && param.Length > "/student/".Length)
                    {
                        string idString = param.Substring("/student/".Length);
                        if (int.TryParse(idString, out int id))
                        {
                            await GetStudentById(response, id);
                            continue;
                        }
                    }

                    if (param == "/student")
                    {
                        string? nameFilter = req.QueryString["Name"];
                        string? groupFilter = req.QueryString["Group"];

                        var filteredStudents = _students.AsEnumerable();

                        if (!string.IsNullOrEmpty(nameFilter))
                        {
                            filteredStudents = filteredStudents.Where(s => s.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
                        }

                        if (!string.IsNullOrEmpty(groupFilter))
                        {
                            filteredStudents = filteredStudents.Where(s => s.Group.Contains(groupFilter, StringComparison.OrdinalIgnoreCase));
                        }

                        await GetFilteredStudents(response, filteredStudents.ToList(), nameFilter, groupFilter);
                        continue;
                    }

                    string page = GetPageName(param);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pages", page);

                    if (!File.Exists(path))
                    {
                        path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pages", "notfound.html");
                    }

                    string html = await File.ReadAllTextAsync(path, Encoding.UTF8);
                    byte[] bytes = Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html; charset=utf-8";
                    response.StatusCode = 200;

                    using (Stream stream = response.OutputStream)
                    {
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    response.Close();
                }
                else if (req.HttpMethod == "POST")
                {
                    string body = "";
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = await reader.ReadToEndAsync();
                        try
                        {
                            if (req.Url?.AbsolutePath == "/student")
                            {
                                var formData = HttpUtility.ParseQueryString(body);

                                int newId = _students.Max(s => s.Id) + 1;
                                Student newStudent = new Student
                                {
                                    Id = newId,
                                    Name = formData["Name"] ?? "",
                                    Surname = formData["Surname"] ?? "",
                                    Group = formData["Group"] ?? ""
                                };

                                _students.Add(newStudent);

                                string successHtml = $"<html><head><meta charset='UTF-8'></head><body><h1>Студента додано!</h1><p>ID: {newId}</p><a href='/student'>Назад до списку</a></body></html>";
                                byte[] bytes = Encoding.UTF8.GetBytes(successHtml);
                                response.ContentType = "text/html; charset=utf-8";
                                response.StatusCode = 200;
                                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                            }
                            else
                            {
                                var formData = HttpUtility.ParseQueryString(body);
                                Console.WriteLine($"Login: {formData["login"]} Password {formData["pwd"]}");
                                response.StatusCode = 302;
                                response.Redirect(_HOST);
                            }
                            response.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            byte[] errorBytes = Encoding.UTF8.GetBytes("<html><body><h1>Помилка</h1></body></html>");
                            response.ContentType = "text/html; charset=utf-8";
                            response.StatusCode = 400;
                            await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                            response.Close();
                        }
                    }
                }
                else if (req.HttpMethod == "PUT")
                {
                    string body = "";
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = await reader.ReadToEndAsync();
                        try
                        {
                            string? path = req.Url?.AbsolutePath;
                            if (path != null && path.StartsWith("/student/") && path.Length > "/student/".Length)
                            {
                                string idString = path.Substring("/student/".Length);
                                if (int.TryParse(idString, out int id))
                                {
                                    var formData = HttpUtility.ParseQueryString(body);
                                    Student? student = _students.FirstOrDefault(s => s.Id == id);

                                    if (student != null)
                                    {
                                        if (!string.IsNullOrEmpty(formData["Name"]))
                                            student.Name = formData["Name"];
                                        if (!string.IsNullOrEmpty(formData["Surname"]))
                                            student.Surname = formData["Surname"];
                                        if (!string.IsNullOrEmpty(formData["Group"]))
                                            student.Group = formData["Group"];

                                        string successHtml = $"<html><head><meta charset='UTF-8'></head><body><h1>Студента оновлено!</h1><p>ID: {id}</p><a href='/student'>Назад до списку</a></body></html>";
                                        byte[] bytes = Encoding.UTF8.GetBytes(successHtml);
                                        response.ContentType = "text/html; charset=utf-8";
                                        response.StatusCode = 200;
                                        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                                    }
                                    else
                                    {
                                        byte[] bytes = Encoding.UTF8.GetBytes($"<html><head><meta charset='UTF-8'></head><body><h1>Помилка</h1><p>Студента з ID {id} не знайдено</p></body></html>");
                                        response.ContentType = "text/html; charset=utf-8";
                                        response.StatusCode = 404;
                                        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                                    }
                                    response.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            response.StatusCode = 500;
                            response.Close();
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
        string html = "<html><head><meta charset='UTF-8'></head><body><h1>Список студентів</h1><ul>";

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

        string html = "<html><head><meta charset='UTF-8'></head><body>";

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

    private async Task GetFilteredStudents(HttpListenerResponse response, List<Student> students, string? nameFilter, string? groupFilter)
    {
        string html = "<html><head><meta charset='UTF-8'></head><body>";
        html += "<h1>Список студентів</h1>";

        if (!string.IsNullOrEmpty(nameFilter) || !string.IsNullOrEmpty(groupFilter))
        {
            html += "<p><strong>Фільтр:</strong> ";
            if (!string.IsNullOrEmpty(nameFilter)) html += $"Ім'я: {nameFilter} ";
            if (!string.IsNullOrEmpty(groupFilter)) html += $"Група: {groupFilter} ";
            html += "</p>";
        }

        if (students.Count == 0)
        {
            html += "<p>Студентів не знайдено за заданим фільтром</p>";
        }
        else
        {
            html += "<ul>";
            foreach (var student in students)
            {
                html += $"<li>ID: {student.Id} - {student.Surname} {student.Name} - Група: {student.Group}</li>";
            }
            html += "</ul>";
        }

        html += "<br><a href='/student'>Скинути фільтр</a>";
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