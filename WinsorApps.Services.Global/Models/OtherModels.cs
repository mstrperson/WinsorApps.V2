﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinsorApps.Services.Global.Models;

public readonly record struct StudentClassName
{
    public static readonly StudentClassName ClassI      = new("Class I");
    public static readonly StudentClassName ClassII     = new("Class II");
    public static readonly StudentClassName ClassIII    = new("Class III");
    public static readonly StudentClassName ClassIV     = new("Class IV");
    public static readonly StudentClassName ClassV      = new("Class V");
    public static readonly StudentClassName ClassVI     = new("Class VI");
    public static readonly StudentClassName ClassVII    = new("Class VII");
    public static readonly StudentClassName ClassVIII   = new("Class VIII");

    public static ReadOnlySpan<StudentClassName> AllClasses => new(
    [
        ClassI,
        ClassII,
        ClassIII,
        ClassIV,
        ClassV,
        ClassVI,
        ClassVII,
        ClassVIII
    ]);

    public static ReadOnlySpan<StudentClassName> LowerSchool => new(
    [
        ClassI,
        ClassII,
        ClassIII,
        ClassIV
    ]);

    public static ReadOnlySpan<StudentClassName> UpperSchool => new(
    [
        ClassV,
        ClassVI,
        ClassVII,
        ClassVIII
    ]);

    public static implicit operator string(StudentClassName name) => name._className;
    public static implicit operator StudentClassName(string str) => str.ToLowerInvariant() switch
    {
        "class I" => ClassI,
        "class II" => ClassII,
        "class III" => ClassIII,
        "class IV" => ClassIV,
        "class V" => ClassV,
        "class VI" => ClassVI,
        "class VII" => ClassVII,
        "class VIII" => ClassVIII,
        _ => throw new InvalidCastException($"{str} is not a valid class name")
    };

    private readonly string _className;

    private StudentClassName(string cn)
    {
        _className = cn;
    }
}