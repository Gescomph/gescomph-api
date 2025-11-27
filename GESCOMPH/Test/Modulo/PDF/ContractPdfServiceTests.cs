using System.Reflection;
using Business.Services.Utilities.PDF;
using Entity.DTOs.Implements.Business.Clause;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.PremisesLeased;
using FluentAssertions;

namespace Test.Modulo.PDF;

public class ContractPdfServiceTests
{
    private static string InvokeBuildHtml(ContractSelectDto dto)
    {
        var mi = typeof(ContractPdfService).GetMethod(
            "BuildHtml",
            BindingFlags.NonPublic | BindingFlags.Static);

        mi.Should().NotBeNull("BuildHtml es un método privado esperado");

        var html = (string)mi!.Invoke(null, new object[] { dto })!;
        return html;
    }

    [Fact]
    public void BuildHtmlReplacesPlaceholdersAndRendersClauses()
    {
        var dto = new ContractSelectDto
        {
            FullName = "Juan & <Ana>",
            Document = "123\"<>&'",
            StartDate = new DateTime(2024, 1, 2),
            EndDate = new DateTime(2025, 2, 3),
            PremisesLeased =
            {
                new PremisesLeasedSelectDto
                {
                    EstablishmentName = "Local <1>",
                    Address = "Calle & 123",
                    AreaM2 = 10,
                    PlazaName = "Plaza 'Central'"
                }
            },
            Clauses =
            {
                new ClauseSelectDto { Name = "A", Description = "Debe <cumplir> & pagar" },
                new ClauseSelectDto { Name = "B", Description = "" },
                new ClauseSelectDto { Name = "C", Description = null! }
            }
        };

        var html = InvokeBuildHtml(dto);

        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("Juan & <Ana>");
        html.Should().Contain("123\"<>&'");
        html.Should().Contain("02/01/2024");
        html.Should().Contain("03/02/2025");
        html.Should().Contain("Local <1>");
        html.Should().Contain("Calle & 123");
        html.Should().Contain(">10<");
        html.Should().Contain("Plaza 'Central'");
        html.Should().Contain("<li>Debe <cumplir> & pagar</li>");
    }

    [Fact]
    public void BuildHtmlRendersMonthlyRentAmountInWords()
    {
        var dto = new ContractSelectDto
        {
            FullName = "Test",
            Document = "123",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2026, 1, 1),
            TotalBaseRentAgreed = 5_700_000,
            TotalUvtQtyAgreed = 38
        };

        var html = InvokeBuildHtml(dto);

        html.Should().Contain("CINCO MILLONES");
        html.Should().Contain("PESOS");
    }

    [Fact]
    public void BuildHtmlRendersDurationInWords()
    {
        var dto = new ContractSelectDto
        {
            FullName = "Duración",
            Document = "999",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 1),
            TotalBaseRentAgreed = 1_000_000,
            TotalUvtQtyAgreed = 10
        };

        var html = InvokeBuildHtml(dto);

        html.Should().Contain("<strong>ONCE</strong> (<strong>11</strong>) MESES");
        html.Should().Contain("<strong>ONCE</strong> (<strong>11</strong>) MESES,");
    }

    [Fact]
    public void BuildHtmlUsesDefaultLandlordValuesWhenNullOrEmpty()
    {
        var dto = new ContractSelectDto
        {
            FullName = "Test",
            Document = "1",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 2)
        };

        var html = InvokeBuildHtml(dto);

        html.Should().Contain("MUNICIPIO DE PALERMO (H)");
        html.Should().Contain("891.180.021-9");
        html.Should().Contain("KLEYVER OVIEDO FARFAN");
        html.Should().Contain("7.717.624");
        html.Should().Contain("Alcalde Municipal");
    }
}
