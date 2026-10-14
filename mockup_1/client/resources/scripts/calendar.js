// A month calendar, shared by the Content Calendar and the Promo Calendar. Load after common.js.
//
//   const calendar = createMonthCalendar(container, onItemClick);
//   calendar.setItems([{ date: "2026-10-15", label: "Bowl", className: "text-bg-success", data: anything }]);
//
// Opens on the current month; the ‹ › buttons move between months. Clicking an item calls
// onItemClick(item.data).
function createMonthCalendar(container, onItemClick) {
    const today = new Date();
    let shown = new Date(today.getFullYear(), today.getMonth(), 1); // the first day of the month shown
    let items = [];

    function render() {
        container.textContent = "";

        // Header: ‹  October 2026  ›   Today
        const header = addElement(container, "div", null, "d-flex align-items-center gap-2 mb-2");
        const prev = addButton(header, "‹", () => move(-1));
        prev.setAttribute("aria-label", "Previous month");
        addElement(header, "h2", shown.toLocaleDateString(undefined, { month: "long", year: "numeric" }), "h4 mb-0 text-center flex-grow-1");
        const next = addButton(header, "›", () => move(1));
        next.setAttribute("aria-label", "Next month");
        addButton(header, "Today", () => {
            shown = new Date(today.getFullYear(), today.getMonth(), 1);
            render();
        });

        const wrapper = addElement(container, "div", null, "table-responsive");
        const table = addElement(wrapper, "table", null, "table table-bordered calendar-table mb-0");
        const headRow = addElement(addElement(table, "thead"), "tr");
        ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].forEach((day) => addElement(headRow, "th", day, "text-center small"));

        // Start on the Sunday on or before the 1st, and fill whole weeks.
        const body = addElement(table, "tbody");
        const day = new Date(shown);
        day.setDate(1 - shown.getDay());
        const todayString = toDayString(today);
        do {
            const row = addElement(body, "tr");
            for (let i = 0; i < 7; i++) {
                row.appendChild(renderDay(day, todayString));
                day.setDate(day.getDate() + 1);
            }
        } while (day.getMonth() === shown.getMonth());
    }

    function renderDay(day, todayString) {
        const dateString = toDayString(day);
        const inMonth = day.getMonth() === shown.getMonth();
        const cell = document.createElement("td");
        if (!inMonth) cell.className = "bg-body-tertiary text-body-secondary";
        if (dateString === todayString) cell.className = "bg-primary-subtle";

        addElement(cell, "div", String(day.getDate()), "small fw-semibold mb-1");
        items.filter((item) => item.date === dateString).forEach((item) => {
            const button = addElement(cell, "button", item.label, "btn btn-sm w-100 text-start text-truncate mb-1 " + item.className);
            button.type = "button";
            button.title = item.label; // the full name, in case it's cut off
            button.addEventListener("click", () => onItemClick(item.data));
        });
        return cell;
    }

    function move(months) {
        shown = new Date(shown.getFullYear(), shown.getMonth() + months, 1);
        render();
    }

    render();
    return {
        setItems(newItems) {
            items = newItems;
            render();
        }
    };
}
