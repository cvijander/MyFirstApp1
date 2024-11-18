using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;
using System.Security.Cryptography.X509Certificates;

namespace MyFirstApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

            var app = builder.Build();

            //app.MapGet("/", () => "Hello World! Pozdrav sa prve stranice");

            //app.MapGet("/about", () => "Ovo je druga stranica tj about");


            app.UseRewriter(new Microsoft.AspNetCore.Rewrite.RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

            //app.Use(async (context, next) =>
            //{
            //    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now} ] Begin transfer");
            //    await next(context);
            //    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.Now}] End transfer");
            //});

            //app.Use(async (context, next) =>
            //{
            //    await context.Response.WriteAsync($"[{context.Request.Method} {context.Request.Path}] Begin transfer\n");
            //    await next(context);
            //    await context.Response.WriteAsync($"[{context.Request.Method} {context.Request.Path}] End transfer\n");
            //});


            var todos = new List<Todo>();

            // vracanje cele liste 
            app.MapGet("/todos", (ITaskService service) => service.GetTodos());

            // vracamo po id
            app.MapGet("todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
            {
                var targetTodo = service.GetTodoById(id);
                return targetTodo is null
                   ? TypedResults.NotFound()
                   : TypedResults.Ok(targetTodo);
            });

            // Create 
            app.MapPost("/todos", (Todo task, ITaskService service) =>
            {
                service.AddTodo(task);
                return TypedResults.Created("/todos/{id}", task);

            })
                .AddEndpointFilter(async (context, next) =>
                {
                    var taskArgument = context.GetArgument<Todo>(0);
                    var errors = new Dictionary<string, string[]>();
                    if (taskArgument.DueDate < DateTime.UtcNow)
                    {
                        errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past"]);
                    }

                    if (taskArgument.IsCompleted)
                    {
                        errors.Add(nameof(Todo.IsCompleted), [" Cannot add completed todo"]);
                    }

                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    return await next(context);
                });

            // Brisanje
            app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
            {
                service.DeleteTodoById(id);
                return TypedResults.NoContent();

            });

            app.Run();          


          
        }

        class InMemoryTaskService : ITaskService
        {
            private readonly List<Todo> _todos = [];

            public Todo AddTodo(Todo task)
            {
                _todos.Add(task);
                return task;
            }

            public void DeleteTodoById (int id)
            {
                _todos.RemoveAll(task => id == task.Id);
            }

            

            public Todo? GetTodoById(int id)
            {
                return _todos.SingleOrDefault(t => id == t.Id);
            }

            public List<Todo> GetTodos()
            {
                return _todos;
            }
        }

        public record Todo(int Id, string Name, DateTime DueDate , bool IsCompleted);

        interface ITaskService
        {
            Todo? GetTodoById(int id);

            List<Todo> GetTodos();

            void DeleteTodoById(int id);

            Todo AddTodo(Todo task);
        }
    }
}
