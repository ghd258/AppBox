import {IComparer} from "../../types/IComparer"
import {IEnumerable} from "../../types/IEnumerable"
import {IOrderedEnumerable} from "../../types/IOrderedEnumerable"
import {OrderedEnumerable} from "../OrderedEnumerable"

/**
 * Sorts the elements of a sequence in ascending order by using a specified or default comparer.
 * @param source A sequence of values to order.
 * @param keySelector A function to extract a key from an element.
 * @param comparer An IComparer<T> to compare keys. Optional.
 * @returns An IOrderedEnumerable<TElement> whose elements are sorted according to a key.
 */
export const orderBy = <TSource, TKey>(
    source: IEnumerable<TSource>,
    keySelector: (x: TSource) => TKey,
    comparer?: IComparer<TKey>): IOrderedEnumerable<TSource> => {
    return OrderedEnumerable.generate<TSource, TKey>(source, keySelector, true, comparer)
}
