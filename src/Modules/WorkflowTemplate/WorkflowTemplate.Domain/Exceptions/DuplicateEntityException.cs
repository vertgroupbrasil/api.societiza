namespace WorkflowTemplate.Domain.Exceptions;

public class DuplicateEntityException(string entity, string property, string value) : Exception($"Duplicate {entity} with {property} '{value}' detected.") 
{
}