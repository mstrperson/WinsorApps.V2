using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinsorApps.Services.Global.Models;

namespace WinsorApps.Services.Helpdesk.Models;
public record CreateCollectionEntry(string assetTag, string chargerAssetTag, bool hasCord, string notes = "");

public record CollectionEntry(string id, string assetTag, string chargerAssetTag, bool hasCord, string notes, DateTime timestamp, UserRecord student);

