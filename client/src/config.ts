/** Cities ingested by the backend (see WeatherIngestion:Cities). Used for filter options. */
export const CITIES = ['London', 'Rome', 'Riga'] as const

/** Data arrives once per minute, so pages refresh on the same cadence. */
export const AUTO_REFRESH_MS = 60_000

/** Neutral, distinguishable series colors (one per city, in alphabetical city order). */
export const SERIES_COLORS = ['#1f4e79', '#8a5a14', '#3b6e47', '#6b3a6b', '#4a5560'] as const
