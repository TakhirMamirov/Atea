import { Link } from 'react-router'
import { PageHeader } from '../components/PageHeader'

export function NotFoundPage() {
  return (
    <>
      <PageHeader title="Page not found" />
      <p>
        <Link to="/">Back to weather</Link>
      </p>
    </>
  )
}
