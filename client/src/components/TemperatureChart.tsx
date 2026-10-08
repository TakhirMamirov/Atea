import { Fragment, useMemo } from 'react'
import {
  Area,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { CitySeries, DateRange } from '../api/types'
import { SERIES_COLORS } from '../config'
import { buildChartData } from '../utils/chartData'
import { formatDateTime, formatShortDateTime, formatTemperature } from '../utils/dateTime'

interface TemperatureChartProps {
  series: CitySeries[]
  range: DateRange
  showMinMax: boolean
}

const TICK_COUNT = 6

/** Evenly spaced ticks across the whole selected range, not just where data exists. */
function buildTicks({ from, to }: DateRange): number[] {
  const step = (to.getTime() - from.getTime()) / (TICK_COUNT - 1)
  return Array.from({ length: TICK_COUNT }, (_, i) => Math.round(from.getTime() + i * step))
}

export function TemperatureChart({ series, range, showMinMax }: TemperatureChartProps) {
  const { rows, keys } = useMemo(() => buildChartData(series), [series])
  const ticks = useMemo(() => buildTicks(range), [range])

  return (
    <figure className="chart" aria-label="Temperature over time for all cities">
      <ResponsiveContainer width="100%" height={380}>
        <ComposedChart data={rows} margin={{ top: 8, right: 16, bottom: 0, left: 0 }}>
          <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" vertical={false} />
          <XAxis
            dataKey="timestamp"
            type="number"
            scale="time"
            domain={[range.from.getTime(), range.to.getTime()]}
            ticks={ticks}
            tickFormatter={formatShortDateTime}
            tick={{ fontSize: 12, fill: 'var(--text-muted)' }}
            stroke="var(--border-strong)"
            minTickGap={24}
          />
          <YAxis
            unit="°C"
            domain={['auto', 'auto']}
            tick={{ fontSize: 12, fill: 'var(--text-muted)' }}
            stroke="var(--border-strong)"
            width={56}
          />
          <Tooltip
            labelFormatter={(value) => formatDateTime(Number(value))}
            formatter={(value, name) =>
              Array.isArray(value)
                ? [`${formatTemperature(Number(value[0]))} – ${formatTemperature(Number(value[1]))}`, name]
                : [formatTemperature(Number(value)), name]
            }
            contentStyle={{ fontSize: 13, borderColor: 'var(--border-strong)', borderRadius: 4 }}
          />
          <Legend wrapperStyle={{ fontSize: 13 }} />
          {keys.map((key, index) => {
            const color = SERIES_COLORS[index % SERIES_COLORS.length]
            return (
              <Fragment key={key.city}>
                {showMinMax && (
                  <Area
                    dataKey={key.rangeKey}
                    name={`${key.city} min–max`}
                    stroke="none"
                    fill={color}
                    fillOpacity={0.12}
                    legendType="none"
                    isAnimationActive={false}
                    connectNulls
                  />
                )}
                <Line
                  dataKey={key.temperatureKey}
                  name={key.label}
                  stroke={color}
                  strokeWidth={1.75}
                  dot={false}
                  isAnimationActive={false}
                  connectNulls
                />
              </Fragment>
            )
          })}
        </ComposedChart>
      </ResponsiveContainer>
    </figure>
  )
}
