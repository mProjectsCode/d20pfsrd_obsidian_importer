namespace d20pfsrd_web_scraper;

public struct TaskRetObj
{
    public readonly NoteMetadata? NoteMetadata;
    public readonly string Md;
    public readonly bool Success;

    public TaskRetObj(bool success)
    {
        Success = success;
        NoteMetadata = null;
        Md = "";
    }

    public TaskRetObj(NoteMetadata noteMetadata, string md)
    {
        NoteMetadata = noteMetadata;
        Md = md;
        Success = true;
    }
}