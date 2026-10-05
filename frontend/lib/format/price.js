// en-US digit grouping with a fixed PKR prefix. en-PK formats differently on Node
// and in browsers, which breaks server-rendered text on hydration.
const formatter = new Intl.NumberFormat("en-US", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function formatPrice(value) {
  return `PKR ${formatter.format(value)}`;
}
