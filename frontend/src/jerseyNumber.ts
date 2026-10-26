/** Fixture IDs encode squad numbers; unknown IDs deliberately get no number. */
export function squadNumber(id: string) {
  return /^(ars|che|lee|liv|mci|tot)-(\d{1,2})$/.exec(id)?.[2] ?? ''
}
