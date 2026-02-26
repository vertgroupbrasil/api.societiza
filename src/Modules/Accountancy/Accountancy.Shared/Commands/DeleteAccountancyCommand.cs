using Core.Application.Messaging.Commands;

namespace Accountancy.Shared.Commands;

public sealed record DeleteAccountancyCommand(Guid Id) : ICommand;