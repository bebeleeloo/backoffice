import type { CountryDto } from "@/api/types";

export function buildCountryDto(overrides: Partial<CountryDto> = {}): CountryDto {
  return {
    id: "1",
    iso2: "US",
    iso3: "USA",
    name: "United States",
    flagEmoji: "\u{1F1FA}\u{1F1F8}",
    ...overrides,
  };
}

export function buildCountryList(): CountryDto[] {
  return [
    buildCountryDto({ id: "1", iso2: "US", iso3: "USA", name: "United States", flagEmoji: "\u{1F1FA}\u{1F1F8}" }),
    buildCountryDto({ id: "2", iso2: "GB", iso3: "GBR", name: "United Kingdom", flagEmoji: "\u{1F1EC}\u{1F1E7}" }),
    buildCountryDto({ id: "3", iso2: "DE", iso3: "DEU", name: "Germany", flagEmoji: "\u{1F1E9}\u{1F1EA}" }),
  ];
}
