import type { FetchLog } from '../api/types'
import { formatDateTime } from '../utils/dateTime'

export function LogsTable({ logs }: { logs: FetchLog[] }) {
  return (
    <table className="table">
      <caption className="visually-hidden">Weather fetch attempts, newest first</caption>
      <thead>
        <tr>
          <th scope="col">Time</th>
          <th scope="col">City</th>
          <th scope="col">Result</th>
          <th scope="col" className="num">HTTP</th>
          <th scope="col" className="num">Duration</th>
          <th scope="col">Message</th>
        </tr>
      </thead>
      <tbody>
        {logs.map((log) => (
          <tr key={log.id}>
            <td className="nowrap">{formatDateTime(log.attemptedAtUtc)}</td>
            <td>{log.city}</td>
            <td>
              <span className={log.isSuccess ? 'status status-ok' : 'status status-fail'}>
                {log.isSuccess ? 'Success' : 'Failure'}
              </span>
            </td>
            <td className="num">{log.httpStatusCode ?? '—'}</td>
            <td className="num">{log.durationMs} ms</td>
            <td className="message">{log.errorMessage ?? ''}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
