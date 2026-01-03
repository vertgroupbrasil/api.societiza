using Core.Application.Messaging.Commands;

namespace Corporate.Shared.Commands.Accountancy;

public sealed record DeleteAccountancyCommand(Guid Id) : ICommand;