using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.EventForms.Models;
public readonly record struct TheaterMenuCategory(string id, string name, ImmutableArray<TheaterMenuItem> items, bool deleted = false);
public readonly record struct TheaterMenuItem(string id, string name, string categoryId, bool deleted = false);

public readonly record struct NewTheaterEvent(string notes, ImmutableArray<string> itemIds);

public readonly record struct TheaterEvent(string eventId, string notes, ImmutableArray<DocumentHeader> attachments, ImmutableArray<TheaterMenuItem> items);
