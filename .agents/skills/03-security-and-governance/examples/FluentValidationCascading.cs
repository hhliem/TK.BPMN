using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseBpm.FormMasterData.Application.Validators;

public record SubmitFormRequestDto(
    Guid DepartmentId,
    Guid PositionId,
    decimal RequestedAmount,
    string Reason
);

public class SubmitFormRequestValidator : AbstractValidator<SubmitFormRequestDto>
{
    public SubmitFormRequestValidator(IMasterDataLookupService lookupService)
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Phòng ban không được để trống.");

        RuleFor(x => x.PositionId)
            .NotEmpty().WithMessage("Chức danh không được để trống.")
            .MustAsync(async (dto, positionId, ct) =>
            {
                // Kiểm tra trực tiếp Database để chống hacker can thiệp F12 sửa ID
                return await lookupService.ValidatePositionBelongsToDeptAsync(dto.DepartmentId, positionId, ct);
            })
            .WithMessage("Chức danh không thuộc thẩm quyền của phòng ban đã chọn.");

        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0).WithMessage("Số tiền yêu cầu phải lớn hơn 0.");
    }
}
