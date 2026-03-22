using System;

namespace MecaPro.Domain.Common;

public class DomainException(string message) : Exception(message);
public class NotFoundException(string name, object key) : DomainException($"{name} ({key}) introuvable.");
public class BusinessRuleViolationException(string message) : DomainException(message);
