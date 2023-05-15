namespace TodoMinimalAPI.Model
{
    public class Todo
    {
        public int UserId { get; set; }
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public bool IsCompleted { get; set; }
    }
}
