﻿namespace Shipwreck.ClickOnce.Manifest;

public interface IFileAssociationSettings
{
    IList<FileAssociation> FileAssociations { get; }
}