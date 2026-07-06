namespace EduCollab.Application.Exceptions
{
    public sealed record InvalidSceneReference(int SceneId, string Reason);

    public sealed class InvalidSceneReferenceException : Exception
    {
        public InvalidSceneReferenceException(IReadOnlyList<InvalidSceneReference> references)
            : base(BuildMessage(references))
        {
            References = references;
        }

        public IReadOnlyList<InvalidSceneReference> References { get; }

        private static string BuildMessage(IReadOnlyList<InvalidSceneReference> references)
        {
            if (references.Count == 0)
                return "One or more scene references are invalid.";

            var details = string.Join(
                ", ",
                references.Select(reference => $"{reference.SceneId}: {reference.Reason}"));

            return $"One or more scene references are invalid: {details}.";
        }
    }
}
