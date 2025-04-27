using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.Services.AddSingleton<ITaskService>(new ConcreteTaskService());

WebApplication webApplication = webApplicationBuilder.Build();

webApplication.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

webApplication.Use(async (context, next) =>{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});

List<ToDo> listOfToDos = new List<ToDo>();

webApplication.MapGet("/todos", (ITaskService taskService) => taskService.GetToDos());

webApplication.MapGet("/todos/{id}", Results<Ok<ToDo>, NotFound> (int id, ITaskService taskService) =>{

    ToDo targetToDo = taskService.GetToDo(id);

    return targetToDo is null ? TypedResults.NotFound() : TypedResults.Ok(targetToDo);

});

webApplication.MapPost("/todos", (ToDo task, ITaskService taskService) =>{

    taskService.AddToDo(task);

    return TypedResults.Created("/todos/{id}", task);

})
.AddEndpointFilter(async (context, next) =>{

    var taskArgument = context.GetArgument<ToDo>(0);
    var errors = new Dictionary<string, string[]>();

    if(taskArgument.DueDate < DateTime.UtcNow)
        errors.Add(nameof(ToDo.DueDate), ["Please note that a past date is invalid."]);
    if(taskArgument.IsCompleted)
        errors.Add(nameof(ToDo.IsCompleted), ["Please note that a completed todo is invalid."]);

    if(errors.Count > 0)
        return Results.ValidationProblem(errors);

    return await next(context);

});

webApplication.MapDelete("/todos/{id}", (int id, ITaskService taskService) =>{

    taskService.DeleteToDo(id);

    return TypedResults.NoContent();

});

webApplication.Run();

public record ToDo(int ID, string Name, DateTime DueDate, bool IsCompleted);

interface ITaskService{

    ToDo? GetToDo(int id);
    List<ToDo> GetToDos();
    void DeleteToDo(int id);
    ToDo AddToDo(ToDo toDo);

}

class ConcreteTaskService : ITaskService{

    private readonly List<ToDo> _toDos = [];

    public ToDo? GetToDo(int id) => _toDos.SingleOrDefault(todo => todo.ID == id);

    public List<ToDo> GetToDos() => _toDos;

    public void DeleteToDo(int id) => _toDos.RemoveAll(toDo => toDo.ID == id);

    public ToDo AddToDo(ToDo toDo){

        _toDos.Add(toDo);

        return toDo;

    }

}