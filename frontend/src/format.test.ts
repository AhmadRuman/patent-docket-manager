import { formatDate, parseDate, relativeDays } from './format'

describe('format', () => {
  it('parses ISO dates as local calendar dates without shifting the day', () => {
    const d = parseDate('2026-10-13')
    expect([d.getFullYear(), d.getMonth(), d.getDate()]).toEqual([2026, 9, 13])
  })

  it('formats dates and handles missing values', () => {
    expect(formatDate('2026-10-13')).toBe('Tue, Oct 13, 2026')
    expect(formatDate(null)).toBe('—')
  })

  it.each([
    [0, 'Due today'],
    [1, 'Tomorrow'],
    [5, 'In 5 days'],
    [-1, '1 day overdue'],
    [-3, '3 days overdue'],
  ])('describes %i days as "%s"', (days, text) => {
    expect(relativeDays(days)).toBe(text)
  })
})
