import * as System from "../../src/System";

describe("DateTime Tests", () => {

    it("Subtract", () => {
        let date1 = new System.DateTime(2020, 1, 1, 1, 1, 1);
        let date2 = new System.DateTime(2020, 1, 1, 1, 1, 2);
        let timespan = date2.Subtract(date1);
        expect(timespan.TotalSeconds).toEqual(1);
    });

});
