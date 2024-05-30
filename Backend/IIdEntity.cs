using System;

namespace Backend;

public interface IIdEntity
{
    Guid Id { get; }
    string Name { get; }
}