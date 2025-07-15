using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.NodeAllocations;
using MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class AllocationMapper
{
    public static partial NodeAllocationResponse ToNodeAllocation(Allocation allocation);
    public static partial Allocation ToAllocation(CreateNodeAllocationRequest request);
    public static partial Allocation Merge(UpdateNodeAllocationRequest request, Allocation allocation);
}