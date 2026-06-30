namespace EduCollab.Application.Exceptions
{
    public sealed record InvalidAssetReference(int AssetId, string Reason);

    public sealed class InvalidAssetReferenceException : Exception
    {
        public InvalidAssetReferenceException(IReadOnlyList<InvalidAssetReference> references)
            : base(BuildMessage(references))
        {
            References = references;
        }

        public IReadOnlyList<InvalidAssetReference> References { get; }

        private static string BuildMessage(IReadOnlyList<InvalidAssetReference> references)
        {
            if (references.Count == 0)
                return "One or more asset references in the scene JSON are invalid.";

            var details = string.Join(
                ", ",
                references.Select(reference => $"{reference.AssetId}: {reference.Reason}"));

            return $"One or more asset references in the scene JSON are invalid: {details}.";
        }
    }
}
