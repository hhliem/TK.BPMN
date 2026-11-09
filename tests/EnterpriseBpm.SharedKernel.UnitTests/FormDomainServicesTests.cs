using EnterpriseBpm.Form.Domain.Exceptions;
using EnterpriseBpm.Form.Domain.Services;
using Xunit;

namespace EnterpriseBpm.SharedKernel.UnitTests;

public class FormDomainServicesTests
{
    [Fact]
    public void Sanitize_WhenValidJson_ShouldReturnOriginalJson()
    {
        var validJson = "{\"components\":[{\"type\":\"textfield\",\"key\":\"fullName\",\"label\":\"Họ và tên\"}]}";
        var result = FormSchemaSanitizer.Sanitize(validJson);
        Assert.Equal(validJson, result);
    }

    [Fact]
    public void Sanitize_WhenInvalidJsonSyntax_ShouldThrowInvalidFormSchemaException()
    {
        var invalidJson = "{ not a valid json ";
        Assert.Throws<InvalidFormSchemaException>(() => FormSchemaSanitizer.Sanitize(invalidJson));
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("{\"label\":\"Tên\",\"default\":\"javascript:void(0)\"}")]
    [InlineData("{\"template\":\"<img src=x onerror=alert(1)>\"}")]
    public void Sanitize_WhenContainsXssOrScript_ShouldThrowInvalidFormSchemaException(string maliciousSchema)
    {
        Assert.Throws<InvalidFormSchemaException>(() => FormSchemaSanitizer.Sanitize(maliciousSchema));
    }

    [Fact]
    public void AntiTampering_WhenChildBelongsToParent_ShouldPass()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var exception = Record.Exception(() => 
            AntiTamperingItemValidator.Validate(childId, parentId, _ => parentId));

        Assert.Null(exception);
    }

    [Fact]
    public void AntiTampering_WhenChildBelongsToDifferentParent_ShouldThrowTamperedDataException()
    {
        var parentA = Guid.NewGuid();
        var parentB = Guid.NewGuid();
        var childId = Guid.NewGuid();

        // Thực tế childId thuộc về parentB, nhưng client gửi nộp parentA
        Assert.Throws<TamperedDataException>(() => 
            AntiTamperingItemValidator.Validate(childId, parentA, _ => parentB));
    }
}

